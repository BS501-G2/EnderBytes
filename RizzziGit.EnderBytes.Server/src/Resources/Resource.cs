namespace RizzziGit.EnderBytes.Resources;

using Framework.Lifetime;

public abstract partial class Resource<M, D, R>(M manager, Resource<M, D, R>.ResourceRecord record) : Lifetime
  where M : Resource<M, D, R>.ResourceManager
  where D : Resource<M, D, R>.ResourceData
  where R : Resource<M, D, R>
{
  private sealed record VersionInformation(string Name, long Version);

  public sealed record ResourceRecord(long Id, long CreateTime, long UpdateTime, D Data);
  public abstract record ResourceData();

  public readonly M Manager = manager;

  private ResourceRecord Record = record;
  protected D Data => Record.Data;

  public long Id => Record.Id;
  public long CreateTime => Record.CreateTime;
  public long UpdateTime => Record.UpdateTime;

  public bool IsValid => Manager.IsValid((R)this);
  public void ThrowIfInvalid()
  {
    lock (this)
    {
      if (!IsValid)
      {
        throw new InvalidOperationException("Invalid resource.");
      }
    }
  }
}
