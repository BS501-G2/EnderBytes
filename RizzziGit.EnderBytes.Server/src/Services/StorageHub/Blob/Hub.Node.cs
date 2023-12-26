using MongoDB.Driver;

namespace RizzziGit.EnderBytes.Services;

using Records;
using Utilities;

public sealed partial class StorageHubService
{
  public abstract partial class Hub
  {
    public sealed partial class Blob : Hub
    {
      private class NodeFolder(Blob hub, long nodeId, long keyId) : Node.Folder(hub, nodeId, keyId)
      {
        private new readonly Blob Hub = hub;

        protected override async Task<File> Internal_CreateFile(string name)
        {
          await Hub.MongoClient.RunTransaction((_) =>
          {
            if (Hub.Nodes.Find((node) => node.ParentNode == NodeId && node.Name == name).First() != null)
            {
              throw new ArgumentException("A node with the same name already exists.", nameof(name));
            }

            (long id, long createTime, long updateTime) = Record.GenerateNewId(Hub.Nodes);
            (byte[] privateKey, byte[] publicKey) = Hub.Server.KeyGeneratorService.GetNewRsaKeyPair();
          });
        }

        protected override Task<Folder> Internal_CreateFolder(string name)
        {
          throw new NotImplementedException();
        }

        protected override Task<SymbolicLink> Internal_CreateSymbolicLink(string name, string[] target)
        {
          throw new NotImplementedException();
        }

        protected override Task<Node[]> Internal_Scan()
        {
          throw new NotImplementedException();
        }
      }
    }
  }
}
