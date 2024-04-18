using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using Services;

public sealed partial class WebApi
{
  [Route("/file/!root/snapshot")]
  [Route("/file/:{id}/snapshot")]
  [HttpGet]
  public Task<ActionResult> GetFileSnapshots(long? id) => HandleFileRoute(new Route_File.Id.Snapshot(id));

  [NonAction]
  public ActionResult HandleFileIdSnapshotRoute(Route_File.Id.Snapshot request, ResourceService.Transaction transaction, FileManager.Resource file, StorageManager.Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken)
  {
    FileSnapshotManager fileSnapshotManager = ResourceService.GetManager<FileSnapshotManager>();

    return request switch
    {
      Route_File.Id.Snapshot.Id fileIdSnapshotIdRequest => HandleFileIdSnapshotIdRoute(fileIdSnapshotIdRequest, transaction, file, storage, userAuthenticationToken, cancellationToken),
      Route_File.Id.Snapshot.Create fileIdSnapshotCreateRequest => HandleFileIdSnapshotCreateRoute(fileIdSnapshotCreateRequest, transaction, file, storage, userAuthenticationToken, cancellationToken),
      _ => Ok(fileSnapshotManager.List(transaction, storage, file, userAuthenticationToken, null, null, cancellationToken).ToArray())
    };
  }

  [NonAction]
  public ActionResult HandleFileIdSnapshotCreateRoute(Route_File.Id.Snapshot.Create request, ResourceService.Transaction transaction, FileManager.Resource file, StorageManager.Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken)
  {
    FileSnapshotManager fileSnapshotManager = ResourceService.GetManager<FileSnapshotManager>();

    if (!fileSnapshotManager.TryGetById(transaction, request.BaseSnapshotId, out FileSnapshotManager.Resource? fileSnapshot, cancellationToken))
    {
      return NotFound();
    }

    return Ok(fileSnapshotManager.Create(transaction, storage, file, fileSnapshot, userAuthenticationToken, cancellationToken));
  }

  [NonAction]
  public ActionResult HandleFileIdSnapshotIdRoute(Route_File.Id.Snapshot.Id request, ResourceService.Transaction transaction, FileManager.Resource file, StorageManager.Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken)
  {
    FileSnapshotManager fileSnapshotManager = ResourceService.GetManager<FileSnapshotManager>();

    if (!fileSnapshotManager.TryGetById(transaction, request.SnapshotId, out FileSnapshotManager.Resource? fileSnapshot, cancellationToken))
    {
      return NotFound();
    }

    return request switch
    {
      Route_File.Id.Snapshot.Id.Upload fileIdSnapshotIdUploadRequest => HandleFileIdSnapshotIdUploadRoute(fileIdSnapshotIdUploadRequest, transaction, fileSnapshot, file, storage, userAuthenticationToken, cancellationToken),
      _ => Ok(fileSnapshot),
    };
  }

  [NonAction]
  public ActionResult HandleFileIdSnapshotIdUploadRoute(Route_File.Id.Snapshot.Id.Upload request, ResourceService.Transaction transaction, FileSnapshotManager.Resource fileSnapshot, FileManager.Resource file, StorageManager.Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken)
  {
    FileBufferMapManager fileBufferMapManager = ResourceService.GetManager<FileBufferMapManager>();
    fileBufferMapManager.Write(transaction, storage, file, fileSnapshot, request.Offset, request.Bytes, userAuthenticationToken, cancellationToken);

    return Ok();
  }
}
