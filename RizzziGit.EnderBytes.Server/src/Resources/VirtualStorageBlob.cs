using System.Data.SQLite;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Resources;

using Buffer;
using Database;

public sealed class VirtualStorageBlobResource(VirtualStorageBlobResource.ResourceManager manager, VirtualStorageBlobResource.ResourceData data) : Resource<VirtualStorageBlobResource.ResourceManager, VirtualStorageBlobResource.ResourceData, VirtualStorageBlobResource>(manager, data)
{
  public const string NAME = "VirtualStorageBlobResource";
  public const int VERSION = 1;

  private const string KEY_STORAGE_POOL_ID = "PoolID";
  private const string KEY_NODE_ID = "NodeID";
  private const string KEY_INDEX = "_Index";
  private const string KEY_BUFFER_START = "BufferStart";
  private const string KEY_BUFFER_END = "BufferEnd";

  private const string INDEX_NODE_ID = $"Index_{NAME}_{KEY_NODE_ID}";
  private const string INDEX_INDEX = $"Index_{NAME}_{KEY_NODE_ID}_{KEY_INDEX}";

  public const string JSON_KEY_NODE_ID = "nodeId";
  public const string JSON_KEY_INDEX = "index";

  public new sealed class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime,
    ulong storagePoolId,
    ulong nodeId,
    ulong index,
    ulong bufferStart,
    ulong bufferEnd
  ) : Resource<ResourceManager, ResourceData, VirtualStorageBlobResource>.ResourceData(id, createTime, updateTime)
  {
    public ulong StoragePoolID = storagePoolId;
    public ulong NodeID = nodeId;
    public ulong Index = index;
    public ulong BufferStart = bufferStart;
    public ulong BufferEnd = bufferEnd;

    public override void CopyFrom(ResourceData data)
    {
      base.CopyFrom(data);

      StoragePoolID = data.StoragePoolID;
      NodeID = data.NodeID;
      Index = data.Index;
      BufferStart = data.BufferStart;
      BufferEnd = data.BufferEnd;
    }

    public override JObject ToJSON()
    {
      JObject jObject = base.ToJSON();

      jObject.Merge(new JObject()
      {
        { JSON_KEY_NODE_ID, NodeID },
        { JSON_KEY_INDEX, Index }
      });

      return jObject;
    }
  }

  public new sealed class ResourceManager : Resource<ResourceManager, ResourceData, VirtualStorageBlobResource>.ResourceManager
  {
    public ResourceManager(MainResourceManager main) : base(main, VERSION, NAME)
    {
      main.VirtualStorageNodes.ResourceDeleteHandlers.Add(async (connection, resource, cancellationToken) =>
      {
        await DbDelete(connection, new() { { KEY_NODE_ID, ("=", resource.ID) } }, cancellationToken);
      });

      ResourceDeleteHandlers.Add((connection, resource, CancellationToken) =>
      {
        WriteToBlobFile(resource.StoragePoolID, resource.BufferStart, Buffer.Allocate((long)resource.BufferEnd - (long)resource.BufferStart));
        return Task.CompletedTask;
      });
    }

    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, ulong createTime, ulong updateTime) => new(
      id, createTime, updateTime,

      (ulong)(long)reader[KEY_STORAGE_POOL_ID],
      (ulong)(long)reader[KEY_NODE_ID],
      (ulong)(long)reader[KEY_INDEX],
      (ulong)(long)reader[KEY_BUFFER_START],
      (ulong)(long)reader[KEY_BUFFER_END]
    );

    protected override VirtualStorageBlobResource CreateResource(ResourceData data) => new(this, data);

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_STORAGE_POOL_ID} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_NODE_ID} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_INDEX} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_BUFFER_START} integer not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_BUFFER_END} integer not null;", cancellationToken);

        await connection.ExecuteNonQueryAsync($"create index {INDEX_NODE_ID} on {NAME}({KEY_NODE_ID});", cancellationToken);
        // await connection.ExecuteNonQueryAsync($"create unique index {INDEX_INDEX} on {NAME}({KEY_NODE_ID},{KEY_INDEX});", cancellationToken);
      }
    }

    public async Task<ResourceStream> Stream(SQLiteConnection connection, VirtualStorageNodeResource node, (int? offset, int length)? limit, CancellationToken cancellationToken)
    {
      if (node.Type != VirtualStorageNodeResource.TYPE_FILE)
      {
        throw new InvalidOperationException();
      }

      return await DbSelect(connection, new() { { KEY_NODE_ID, ("=", node.ID) } }, limit, null, cancellationToken);
    }

    public async Task<ulong> FindFreeBlobArea(SQLiteConnection connection, VirtualStorageNodeResource node, ulong length, CancellationToken cancellationToken)
    {
      ulong start = 0;

      await using var stream = await DbSelect(connection, new()
      {
        { KEY_STORAGE_POOL_ID, ("=", node.StoragePoolID) }
      }, null, (KEY_BUFFER_START, "asc"), cancellationToken);

      await foreach (VirtualStorageBlobResource blob in stream)
      {
        if ((blob == null) || (blob.BufferStart - start) >= length)
        {
          break;
        }

        start = blob.BufferEnd;
      }

      return start;
    }

    private string ResolveBlobFile(ulong storagePoolId)
    {
      string path = Path.Join(Main.Server.Config.DatabaseDir, "VirtualStorageBlobs", $"{storagePoolId}");

      if (!Directory.Exists(path))
      {
        Directory.CreateDirectory(path);
      }

      return Path.Join(path, "blob");
    }

    private Buffer ReadFromBlobFile(ulong storagePoolId, ulong start, ulong length)
    {
      string blobFile = ResolveBlobFile(storagePoolId);
      FileStream fileStream = File.Open(blobFile, FileMode.OpenOrCreate, FileAccess.Read);

      try
      {
        fileStream.Seek((long)start, SeekOrigin.Begin);
        Buffer output = Buffer.Empty();

        for (ulong offset = 0; offset < length; offset += 4096)
        {
          byte[] oldBytes = new byte[length];

          int oldBytesLength = fileStream.Read(oldBytes, 0, oldBytes.Length);
          if (oldBytesLength == 0)
          {
            break;
          }

          output.Append(oldBytes, 0, oldBytesLength);
        }

        return output;
      }
      finally
      {
        fileStream.Close();
      }
    }

    private void WriteToBlobFile(ulong storagePoolId, ulong start, Buffer buffer)
    {
      string blobFile = ResolveBlobFile(storagePoolId);
      FileStream fileStream = File.Open(blobFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);

      try
      {
        fileStream.Seek((long)start, SeekOrigin.Begin);
        byte[] oldBytes = new byte[buffer.Length];
        int oldBytesLength = fileStream.Read(oldBytes, 0, oldBytes.Length);

        fileStream.Seek((long)start, SeekOrigin.Begin);
        byte[] newBytes = buffer.ToByteArray();
        fileStream.Write(newBytes, 0, newBytes.Length);

        Database.RegisterOnTransactionCompleteHandlers(null, (connection) =>
        {
          FileStream fileStream = File.Open(blobFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);

          try
          {
            fileStream.Seek((long)start, SeekOrigin.Begin);
            fileStream.Write(oldBytes, 0, oldBytesLength);
          }
          finally
          {
            fileStream.Close();
          }

          return Task.CompletedTask;
        });
      }
      finally
      {
        fileStream.Close();
      }
    }

    public async Task<VirtualStorageBlobResource> Insert(SQLiteConnection connection, VirtualStorageNodeResource node, ulong offset, Buffer buffer, CancellationToken cancellationToken)
    {
      List<VirtualStorageBlobResource> virtualStorageBlobResources = [];
      {
        await using var stream = await DbSelect(connection, new()
        {
          { KEY_STORAGE_POOL_ID, ("=", node.StoragePoolID) },
          { KEY_NODE_ID, ("=", node.ID) }
        }, null, (KEY_INDEX, "asc"), cancellationToken);

        await foreach (VirtualStorageBlobResource blob in stream)
        {
          virtualStorageBlobResources.Add(blob);
        }
      }

      Buffer newBuffer = Buffer.Empty();
      {
        ulong bufferOffset = 0;
        for (int index = 0; index < virtualStorageBlobResources.Count; index++)
        {
          VirtualStorageBlobResource blob = virtualStorageBlobResources[index];

          if (bufferOffset > 0)
          {
            if (bufferOffset >= blob.Length)
            {
              bufferOffset -= blob.Length;
              continue;
            }

            newBuffer.Append(buffer.Slice((long)bufferOffset));
            newBuffer.Append(ReadFromBlobFile(blob.StoragePoolID, blob.BufferStart + bufferOffset, bufferOffset));

            WriteToBlobFile(blob.StoragePoolID, blob.BufferStart + bufferOffset, buffer.Slice(0, (long)bufferOffset));
          }

          break;
        }
      }

      {
        ulong start = await FindFreeBlobArea(connection, node, (ulong)newBuffer.Length, cancellationToken);

        VirtualStorageBlobResource virtualStorageBlobResource = await DbInsert(connection, new()
        {
          { KEY_STORAGE_POOL_ID, node.StoragePoolID },
          { KEY_NODE_ID, node.ID },
          { KEY_INDEX, node.BlobCount },
          { KEY_BUFFER_START, start },
          { KEY_BUFFER_END, start + (ulong)newBuffer.Length }
        }, cancellationToken);
        await node.SetBlobCount(connection, node.BlobCount + 1, cancellationToken);
        await node.SetLength(connection, node.Length + (ulong)newBuffer.Length, cancellationToken);

        WriteToBlobFile(node.StoragePoolID, start, newBuffer);
        return virtualStorageBlobResource;
      }
    }

    public async Task<VirtualStorageBlobResource> Append(SQLiteConnection connection, VirtualStorageNodeResource node, Buffer buffer, CancellationToken cancellationToken)
    {
      ulong start = await FindFreeBlobArea(connection, node, (ulong)buffer.Length, cancellationToken);

      VirtualStorageBlobResource virtualStorageBlobResource = await DbInsert(connection, new()
      {
        { KEY_STORAGE_POOL_ID, node.StoragePoolID },
        { KEY_NODE_ID, node.ID },
        { KEY_INDEX, node.BlobCount },
        { KEY_BUFFER_START, start },
        { KEY_BUFFER_END, start + (ulong)buffer.Length }
      }, cancellationToken);
      await node.SetBlobCount(connection, node.BlobCount + 1, cancellationToken);
      await node.SetLength(connection, node.Length + (ulong)buffer.Length, cancellationToken);

      WriteToBlobFile(node.StoragePoolID, start, buffer);
      return virtualStorageBlobResource;
    }

    public Buffer Read(VirtualStorageBlobResource blob) => ReadFromBlobFile(blob.StoragePoolID, blob.BufferStart, blob.Length);
  }

  public ulong StoragePoolID => Data.StoragePoolID;
  public ulong NodeID => Data.NodeID;
  public ulong Index => Data.Index;
  public ulong BufferStart => Data.BufferStart;
  public ulong BufferEnd => Data.BufferEnd;

  public ulong Length => BufferEnd - BufferStart;

  public Buffer Read() => Manager.Read(this);
}
