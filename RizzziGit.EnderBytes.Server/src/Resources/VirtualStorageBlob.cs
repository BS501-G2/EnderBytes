using System.Data.SQLite;
using Newtonsoft.Json.Linq;

namespace RizzziGit.EnderBytes.Resources;

using Buffer;
using Database;

public sealed class VirtualStorageBlobResource(VirtualStorageBlobResource.ResourceManager manager, VirtualStorageBlobResource.ResourceData data) : Resource<VirtualStorageBlobResource.ResourceManager, VirtualStorageBlobResource.ResourceData, VirtualStorageBlobResource>(manager, data)
{
  public const string NAME = "Name";
  public const int VERSION = 1;

  private const string KEY_BUFFER = "Buffer";
  private const string KEY_NODE_ID = "NodeID";

  private const string INDEX_NODE_ID = $"Index_{NAME}_{KEY_NODE_ID}";

  public const string JSON_KEY_NODE_ID = "nodeId";
  public const string JSON_KEY_INDEX = "index";

  public new sealed class ResourceData(
    ulong id,
    ulong createTime,
    ulong updateTime,
    byte[] buffer,
    ulong nodeId
  ) : Resource<ResourceManager, ResourceData, VirtualStorageBlobResource>.ResourceData(id, createTime, updateTime)
  {
    public byte[] Buffer = buffer;
    public ulong NodeID = nodeId;

    public override void CopyFrom(ResourceData data)
    {
      base.CopyFrom(data);

      Buffer = data.Buffer;
    }

    public override JObject ToJSON()
    {
      JObject jObject = base.ToJSON();

      jObject.Merge(new JObject()
      {
        { JSON_KEY_NODE_ID, NodeID }
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
    }

    protected override ResourceData CreateData(SQLiteDataReader reader, ulong id, ulong createTime, ulong updateTime) => new(
      id, createTime, updateTime,

      (byte[])reader[KEY_BUFFER],
      (ulong)(long)reader[KEY_NODE_ID]
    );

    protected override VirtualStorageBlobResource CreateResource(ResourceData data) => new(this, data);

    protected override Task OnInit(SQLiteConnection connection, CancellationToken cancellationToken) => OnInit(connection, 0, cancellationToken);
    protected override async Task OnInit(SQLiteConnection connection, int previousVersion, CancellationToken cancellationToken)
    {
      if (previousVersion < 1)
      {
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_BUFFER} blob not null;", cancellationToken);
        await connection.ExecuteNonQueryAsync($"alter table {NAME} add column {KEY_NODE_ID} integer not null;", cancellationToken);

        await connection.ExecuteNonQueryAsync($"create index {INDEX_NODE_ID} on {NAME}({KEY_NODE_ID});", cancellationToken);
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

    public Task<VirtualStorageBlobResource> Create(SQLiteConnection connection, VirtualStorageNodeResource node, ulong index, Buffer buffer, CancellationToken cancellationToken) => DbInsert(connection, new()
    {
      { KEY_NODE_ID, node.ID },
      { KEY_BUFFER, buffer.ToByteArray() }
    }, cancellationToken);
  }

  public byte[] Buffer => Data.Buffer;
  public ulong NodeID => Data.NodeID;
}
