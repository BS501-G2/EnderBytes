using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Resources;

using Framework.Lifetime;
using Framework.Collections;
using Utilities;
using Services.Resource;
using System.Collections;
using System.Collections.Concurrent;

public abstract class Resource<M, D, R>(M manager, Resource<M, D, R>.ResourceRecord record) : Lifetime($"#{record.Id}", manager.Logger)
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  private sealed record VersionInformation(string Name, long Version);

  public sealed record ResourceRecord(long Id, long CreateTime, long UpdateTime, D Data);
  public abstract record ResourceData();

  public abstract class ResourceManager(ResourceService service, IMongoDatabase database, string name, long version) : Lifetime(name, service.Logger)
  {
    public readonly ResourceService Service = service;
    public readonly long Version = version;

    public Server Server => Service.Server;

    private IMongoCollection<VersionInformation> VersionCollection => MongoDatabase.GetCollection<VersionInformation>("_Versions");

    private readonly IMongoDatabase MongoDatabase = database;
    private readonly WeakDictionary<long, R> Resources = [];

    private IMongoClient MongoClient => MongoDatabase.Client;
    protected IMongoCollection<ResourceRecord> Collection => MongoDatabase.GetCollection<ResourceRecord>(Name);

    public bool IsValid(R resource)
    {
      try
      {
        return Run(() => Resources.TryGetValue(resource.Id, out R? value) && value == resource);
      }
      catch
      {
        return false;
      }
    }

    protected abstract R CreateResourceClass(ResourceRecord record);
    protected R ResolveResource(ResourceRecord record)
    {
      lock (Resources)
      {
        if (!Resources.TryGetValue(record.Id, out R? resource))
        {
          resource = CreateResourceClass(record);
        }

        resource.Record = record;
        return resource;
      }
    }

    protected ResourceRecord CreateNewRecord(D data, CancellationToken cancellationToken = default) => Run<ResourceRecord>((cancellationToken) =>
    {
      long createTime, updateTime = createTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

      return new(Collection.GenerateNewId(cancellationToken), createTime, updateTime, data);
    }, cancellationToken);

    private static IClientSessionHandle? CurrentSessionHandle;
    protected void RunTransaction(Action<CancellationToken> callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default) => Run((cancellationToken) =>
    {
      cancellationToken.ThrowIfCancellationRequested();
      IMongoClient client = Collection.Database.Client;

      lock (client)
      {
        if (CurrentSessionHandle != null)
        {
          callback(cancellationToken);
          return;
        }

        try
        {
          using IClientSessionHandle clientSession = CurrentSessionHandle = client.StartSession(sessionOptions, cancellationToken);
          List<Action> onFailureCallbacks = [];

          try
          {
            clientSession.StartTransaction(transactionOptions);

            List<ChangeStreamDocument<ResourceRecord>> changes = [];
            CancellationTokenSource changesCancellationTokenSource = new();
            try
            {
              _ = Collection.WatchAsync(changes.Add, cancellationToken);
              callback(cancellationToken);
            }
            finally
            {
              changesCancellationTokenSource.Cancel();
            }

            changes.ForEach((change) =>
            {
              OnChange(change, ResolveResource(change.FullDocument), out List<Action> onFailure);
              onFailureCallbacks.InsertRange(0, onFailure.Reverse<Action>());
            });

            clientSession.CommitTransaction(cancellationToken);
          }
          catch
          {
            foreach (Action onFailure in onFailureCallbacks)
            {
              try { onFailure(); } catch { }
            }

            try { clientSession.AbortTransaction(default); } catch { }
            throw;
          }
        }
        finally
        {
          CurrentSessionHandle = null;
        }
      }
    }, cancellationToken);

    protected IEnumerable<T> RunTransaction<T>(Func<CancellationToken, IEnumerable<T>> callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default)
    {
      cancellationToken.ThrowIfCancellationRequested();

      BlockingCollection<(StrongBox<T>? box, Exception? exception)> shuttle = new(1);
      _ = Task.Run(() => RunTransaction((cancellationToken) =>
      {
        IEnumerable<T> enumerable = callback(cancellationToken);
        using IEnumerator<T> enumerator = enumerable.GetEnumerator();

        for (; ; )
        {
          try
          {
            if (!enumerator.MoveNext())
            {
              shuttle.Add((null, null), cancellationToken);
              return;
            }

            shuttle.Add((new(enumerator.Current), null), cancellationToken);
          }
          catch (Exception exception)
          {
            try { shuttle.Add((null, exception), cancellationToken); } catch { }
            return;
          }
        }
      }, sessionOptions, transactionOptions, cancellationToken), cancellationToken);

      for (; ; )
      {
        (StrongBox<T>? box, Exception? exception) item;

        try { item = shuttle.Take(cancellationToken); }
        catch (OperationCanceledException) { throw; }
        catch { yield break; }

        if (item.box == null)
        {
          yield break;
        }

        yield return item.box.Value!;
      }
    }

    protected T RunTransaction<T>(Func<CancellationToken, T> callback, ClientSessionOptions? sessionOptions = null, TransactionOptions? transactionOptions = null, CancellationToken cancellationToken = default)
    {
      StrongBox<T>? box = null;
      RunTransaction((cancellationToken) => { box = new(callback(cancellationToken)); }, sessionOptions, transactionOptions, cancellationToken);
      return box!.Value!;
    }

    protected void OnChange(ChangeStreamDocument<ResourceRecord> change, R resource, out List<Action> onFailure)
    {
      onFailure = [];

      if (change.OperationType == ChangeStreamOperationType.Delete)
      {
        Resources.Remove(resource.Id);
        onFailure.Add(() => Resources.Add(resource.Id, resource));
      }
      else if (change.OperationType == ChangeStreamOperationType.Insert)
      {
        onFailure.Add(() => Resources.Remove(resource.Id));
      }
      else if (change.OperationType == ChangeStreamOperationType.Update || change.OperationType == ChangeStreamOperationType.Replace)
      {
        onFailure.Add(() => resource.Record = change.FullDocumentBeforeChange);
      }
    }

    protected virtual void OnUpgrade(long oldVersion = default, CancellationToken cancellationToken = default) { }

    protected override Task OnRun(CancellationToken cancellationToken)
    {
      checkForUpgrade(cancellationToken);

      return Task.Delay(-1, cancellationToken);

      void checkForUpgrade(CancellationToken cancellationToken)
      {
        MongoClient.RunTransaction((cancellationToken) =>
        {
          long? version = VersionCollection.FindOne((record) => record.Name == Name, cancellationToken: cancellationToken)?.Version;
          if (Version != version)
          {
            OnUpgrade(version ?? default, cancellationToken);
          }

          if (version == null)
          {
            VersionCollection.InsertOne(new(Name, Version), cancellationToken: cancellationToken);
          }
          else
          {
            VersionCollection.UpdateOne((record) => record.Name == Name, Builders<VersionInformation>.Update.Set((record) => record.Version, version), cancellationToken: cancellationToken);
          }

        }, cancellationToken: cancellationToken);
      }
    }

    public R? GetById(long id, CancellationToken cancellationToken = default) => RunTransaction((cancellationToken) =>
    {
      ResourceRecord? record = Collection.FindOne((record) => record.Id == id, cancellationToken: cancellationToken);
      return record != null ? ResolveResource(record) : null;
    }, cancellationToken: cancellationToken);

    protected R Insert(D data, CancellationToken cancellationToken = default) => RunTransaction((cancellationToken) =>
    {
      long newId = Collection.GenerateNewId(cancellationToken);
      long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

      ResourceRecord record = new(newId, timestamp, timestamp, data);
      return ResolveResource(record);
    }, cancellationToken: cancellationToken);

    protected R Update(R resource, D newData, CancellationToken cancellationToken = default) => RunTransaction((cancellationToken) =>
    {
      Update((record) => record.Id == resource.Id, newData, cancellationToken);
      return resource;
    }, null, null, cancellationToken);

    protected bool Update(Expression<Func<ResourceRecord, bool>> expression, D newData, CancellationToken cancellationToken = default) => RunTransaction((cancellationToken) => Collection.UpdateOne(
      expression,
      Builders<ResourceRecord>.Update
        .Set((record) => record.Data, newData)
        .Set((record) => record.UpdateTime, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
      null,
      cancellationToken
    ).MatchedCount != 0, cancellationToken: cancellationToken);

    protected void Delete(Expression<Func<ResourceRecord, bool>> expression, CancellationToken cancellationToken = default) => RunTransaction((cancellationToken) => Collection.DeleteMany(expression, cancellationToken), null, null, cancellationToken);
    protected void Delete(R resource, CancellationToken cancellationToken = default) => RunTransaction((cancellationToken) => Collection.DeleteOne((record) => record.Id == resource.Id, null, cancellationToken), null, null, cancellationToken);

    public override void Stop()
    {
      Run(Resources.Clear);

      base.Stop();
    }
  }

  public readonly M Manager = manager;

  private ResourceRecord Record = record;
  protected D Data => Record.Data;

  public long Id => Record.Id;
  public long CreateTime => Record.CreateTime;
  public long UpdateTime => Record.UpdateTime;

  public bool IsValid => Manager.IsValid((R)this);
}
