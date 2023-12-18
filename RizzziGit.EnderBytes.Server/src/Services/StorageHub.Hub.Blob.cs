namespace RizzziGit.EnderBytes.Services;

public enum BlobNodeType { File, Folder, SymbolicLink }

public sealed partial class StorageHubService
{
  public abstract partial class Hub
  {
    public sealed class Blob(StorageHubService service, long hubId, KeyGeneratorService.Transformer.Key hubKey) : Hub(service, hubId, hubKey)
    {
      protected override Task<NodeInformation.File> Internal_FileCreate(long parentFolderNodeId, string name)
      {
        throw new NotImplementedException();
      }

      protected override Task<FileHandle> Internal_FileOpen(long fileNodeId, long snapshotId, HubFileAccess access)
      {
        throw new NotImplementedException();
      }

      protected override Task<NodeInformation.File.Snapshot> Internal_FileSnapshotCreate(long fileNodeId, long? baseSnapshotId, long authorUserId)
      {
        throw new NotImplementedException();
      }

      protected override Task<NodeInformation.File.Snapshot[]> Internal_FileSnapshotScan(long fileNodeId)
      {
        throw new NotImplementedException();
      }

      protected override Task<NodeInformation.Folder> Internal_FolderCreate(long parentFolderNodeId, string name)
      {
        throw new NotImplementedException();
      }

      protected override Task<NodeInformation[]> Internal_FolderScan(long folderNodeId)
      {
        throw new NotImplementedException();
      }

      protected override Task Internal_NodeDelete(long nodeId)
      {
        throw new NotImplementedException();
      }

      protected override Task<NodeInformation> Internal_NodeInfo(long nodeId)
      {
        throw new NotImplementedException();
      }

      protected override Task<long> Internal_ResolveNodeId(string[] path)
      {
        throw new NotImplementedException();
      }

      protected override Task<NodeInformation.SymbolicLink> Internal_SymbolicLinkCreate(long parentFolderNodeId, string[] target, bool replace)
      {
        throw new NotImplementedException();
      }

      protected override Task<string[]> Internal_SymbolicLinkRead(long symbolicLinkNodeId)
      {
        throw new NotImplementedException();
      }
    }
  }
}
