using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using RizzziGit.Commons.Memory;
using Services;

public sealed partial class WebApi
{
  [Route("~/file/!root/snapshots")]
  [Route("~/file/:{id}/snapshots")]
  [HttpGet]
  public async Task<ObjectResult> GetFileSnapshots(long? id)
  {
    ObjectResult fileResult = await GetFileById(id);
    if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
    {
      return fileResult;
    }

    return await Run(async () =>
    {

      StorageManager.Resource storage = CurrentStorage;

      return await ResourceService.Transact<Result>(async (transaction, cancellationToken) =>
      {
        return Data(await transaction.GetManager<FileSnapshotManager>().List(transaction, storage, file, CurrentUserAuthenticationToken, null, null, cancellationToken).ToArrayAsync(cancellationToken));
      });
    });
  }

  public sealed record CreateFileSnapshotRequest(long? BaseSnapshotId);

  public WebApiContext.InstanceHolder<FileSnapshotManager.Resource> CurrentFileSnapshot => new(this, nameof(CurrentFileSnapshot));

  [Route("~/file/:{fileId}/snapshots/:{fileSnapshotId}")]
  [HttpGet]
  public async Task<ObjectResult> GetFileSnapshotById(long fileId, long fileSnapshotId)
  {
    ObjectResult fileResult = await GetFileById(fileId);
    if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
    {
      return fileResult;
    }

    return await Run(async () =>
    {
      return await ResourceService.Transact<Result>(async (transaction, cancellationToken) =>
      {
        FileSnapshotManager.Resource? fileSnapshot;
        if ((fileSnapshot = await transaction.GetManager<FileSnapshotManager>().GetById(transaction, fileSnapshotId, cancellationToken)) == null)
        {
          return Error(404);
        }

        return Data(CurrentFileSnapshot.Set(fileSnapshot));
      });
    });
  }

  [Route("~/file/:{fileId}/snapshots/:{fileSnapshotId}/content")]
  [HttpGet]
  public async Task<ActionResult> GetFileSnapshotContent(long fileId, long fileSnapshotId)
  {
    ObjectResult fileSnapshotResult = await GetFileSnapshotById(fileId, fileSnapshotId);
    if (!TryGetValueFromResult(fileSnapshotResult, out FileSnapshotManager.Resource? fileSnapshot))
    {
      return fileSnapshotResult;
    }

    CompositeBuffer bytes = await ResourceService.Transact(async (transaction, cancellationToken) =>
    {
      long size = await transaction.GetManager<FileBufferMapManager>().GetSize(transaction, CurrentStorage, CurrentFile, fileSnapshot, cancellationToken);
      return await transaction.GetManager<FileBufferMapManager>().Read(transaction, CurrentStorage, CurrentFile, fileSnapshot, 0, size, CurrentUserAuthenticationToken, cancellationToken);
    });

    return File(bytes.ToByteArray(), "application/octet-stream", ((FileManager.Resource)CurrentFile).Name, true);
  }
}
