using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Commons.Memory;
using Resources;
using Services;

public sealed partial class WebApi
{
  [Route("/file/!root")]
  [Route("/file/:{id}")]
  [HttpGet]
  public Task<ActionResult> GetFile(long? id) => HandleFileRoute(new Route_File.Id(id));

  public record Route_File()
  {
    public record Id(long? FileId) : Route_File()
    {
      public record Share(long? FileId) : Id(FileId)
      {
        public new record Id(long? FileId, long ShareId) : Share(FileId);

        public record Create(long? FileId, FileAccessType FileAccessType, long TargetUserId) : Share(FileId);
      }

      public record Files(long? FileId) : Id(FileId)
      {
        public record Create(long? FileId, bool IsFile, string Name) : Files(FileId);
      }

      public record Snapshot(long? FileId) : Id(FileId)
      {
        public new record Id(long? FileId, long SnapshotId) : Snapshot(FileId)
        {
          public record Upload(long? FileId, long SnapshotId, long Offset, CompositeBuffer Bytes) : Id(FileId, SnapshotId);
        }

        public record Create(long? FileId, long BaseSnapshotId) : Snapshot(FileId);
      }
    }
  }

  [NonAction]
  public async Task<ActionResult> HandleFileRoute(Route_File request)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return await ResourceService.Transact(async (transaction, cancellationToken) => request switch
    {
      Route_File.Id fileIdRequest => await HandleFileIdRoute(fileIdRequest, transaction, userAuthenticationToken, cancellationToken),

      _ => BadRequest(),
    });
  }

  [NonAction]
  public async Task<ActionResult> HandleFileIdRoute(Route_File.Id request, ResourceService.Transaction transaction, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken)
  {
    StorageManager storageManager = ResourceService.GetManager<StorageManager>();
    FileManager fileManager = ResourceService.GetManager<FileManager>();

    FileManager.Resource? file;
    StorageManager.Resource? storage;

    if (request.FileId == null)
    {
      storage = await storageManager.GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);
      file = await storageManager.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);
    }
    else if (
      ((file = await fileManager.GetById(transaction, (long)request.FileId, cancellationToken)) == null) ||
      ((storage = await storageManager.GetById(transaction, file.StorageId, cancellationToken)) == null)
    )
    {
      return NotFound();
    }

    DecryptedKeyInfo decryptedKeyInfo = await storageManager.DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);

    if (decryptedKeyInfo.FileAccess == null)
    {
      FileManager.Resource rootFolder = await storageManager.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);

      if (!await fileManager.IsEqualToOrInsideOf(transaction, storage, rootFolder, file, cancellationToken))
      {
        return Forbid();
      }
    }

    return request switch
    {
      Route_File.Id.Files fileIdFilesRequest => await HandleFileIdFilesRoute(fileIdFilesRequest, transaction, file, storage, userAuthenticationToken, cancellationToken),
      Route_File.Id.Snapshot fileIdSnapshotRequest => await HandleFileIdSnapshotRoute(fileIdSnapshotRequest, transaction, file, storage, userAuthenticationToken, cancellationToken),

      _ => Ok(file),
    };
  }
}
