using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Commons.Memory;

using Resources;

public sealed partial class WebApi
{
  [Route("/file/:{fileId}/snapshots")]
  [HttpGet]
  public async Task<ActionResult<FileSnapshotManager>> ListSnapshots(long fileId)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    FileManager files = ResourceService.GetManager<FileManager>();
    StorageManager storages = ResourceService.GetManager<StorageManager>();
    FileSnapshotManager fileSnapshots = ResourceService.GetManager<FileSnapshotManager>();

    return await ResourceService.Transact<ActionResult>((transaction, cancellationToken) =>
    {
      if (
        !files.TryGetById(transaction, fileId, out FileManager.Resource? file, cancellationToken) ||
        !storages.TryGetById(transaction, file.StorageId, out StorageManager.Resource? storage, cancellationToken)
      )
      {
        return NotFound();
      }
      else if (!files.IsEqualToOrInsideOf(transaction, storage, storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken), file, cancellationToken))
      {
        return Forbid();
      }

      return Ok(fileSnapshots.List(transaction, storage, file, userAuthenticationToken, cancellationToken: cancellationToken));
    });
  }

  [Route("/file/:{fileId}/snapshots/:{snapshotId}")]
  [HttpGet]
  public async Task<ActionResult> GetSnapshot(long fileId, long snapshotId)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    FileManager files = ResourceService.GetManager<FileManager>();
    StorageManager storages = ResourceService.GetManager<StorageManager>();
    FileSnapshotManager fileSnapshots = ResourceService.GetManager<FileSnapshotManager>();

    return await ResourceService.Transact<ActionResult>((transaction, cancellationToken) =>
    {
      if (
        !files.TryGetById(transaction, fileId, out FileManager.Resource? file, cancellationToken) ||
        !storages.TryGetById(transaction, file.StorageId, out StorageManager.Resource? storage, cancellationToken)
      )
      {
        return NotFound();
      }
      else if (!files.IsEqualToOrInsideOf(transaction, storage, storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken), file, cancellationToken))
      {
        return Forbid();
      }
      else if (!fileSnapshots.TryGetById(transaction, snapshotId, out FileSnapshotManager.Resource? snapshot, cancellationToken))
      {
        return NotFound();
      }
      else
      {
        return Ok(snapshot);
      }
    });
  }

  public sealed record CreateSnapshotRequest(long? BaseSnapshotId);

  [Route("/file/:{fileId}/snapshots")]
  [HttpPost]
  public async Task<ActionResult<FileSnapshotManager.Resource>> CreateSnapshot(long fileId, [FromBody] CreateSnapshotRequest request)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    FileManager files = ResourceService.GetManager<FileManager>();
    StorageManager storages = ResourceService.GetManager<StorageManager>();
    FileSnapshotManager fileSnapshots = ResourceService.GetManager<FileSnapshotManager>();

    return await ResourceService.Transact<ActionResult>((transaction, cancellationToken) =>
    {
      if (
        !files.TryGetById(transaction, fileId, out FileManager.Resource? file, cancellationToken) ||
        !storages.TryGetById(transaction, file.StorageId, out StorageManager.Resource? storage, cancellationToken)
      )
      {
        return NotFound();
      }
      else if (!files.IsEqualToOrInsideOf(transaction, storage, storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken), file, cancellationToken))
      {
        return Forbid();
      }
      else
      {
        if (request.BaseSnapshotId == null && fileSnapshots.List(transaction, storage, file, userAuthenticationToken, cancellationToken: cancellationToken).Any())
        {
          return Forbid();
        }

        FileSnapshotManager.Resource? baseSnapshot = null;

        if (request.BaseSnapshotId != null && !fileSnapshots.TryGetById(transaction, (long)request.BaseSnapshotId, out baseSnapshot, cancellationToken))
        {
          return NotFound();
        }

        FileSnapshotManager.Resource snapshot = fileSnapshots.Create(transaction, storage, file, baseSnapshot, userAuthenticationToken, cancellationToken);
        return Ok(snapshot);
      }
    });
  }

  public sealed record UploadContentRequest(long Offset, IFormFile Content);

  [Route("/file/:{fileId}/snapshots/:{snapshotId}")]
  [HttpPost]
  public async Task<ActionResult<FileSnapshotManager.Resource>> UploadContent(long fileId, long snapshotId, [FromBody] UploadContentRequest request)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    FileManager files = ResourceService.GetManager<FileManager>();
    StorageManager storages = ResourceService.GetManager<StorageManager>();
    FileSnapshotManager fileSnapshots = ResourceService.GetManager<FileSnapshotManager>();
    FileBufferMapManager fileBufferMaps = ResourceService.GetManager<FileBufferMapManager>();

    CompositeBuffer buffer = [];

    {
      using MemoryStream stream = new();
      await request.Content.CopyToAsync(stream);
      buffer.Append(stream.ToArray());
    }

    return await ResourceService.Transact<ActionResult>((transaction, cancellationToken) =>
    {
      if (
        !files.TryGetById(transaction, fileId, out FileManager.Resource? file, cancellationToken) ||
        !storages.TryGetById(transaction, file.StorageId, out StorageManager.Resource? storage, cancellationToken)
      )
      {
        return NotFound();
      }
      else if (!files.IsEqualToOrInsideOf(transaction, storage, storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken), file, cancellationToken))
      {
        return Forbid();
      }
      else
      {
        if (!fileSnapshots.TryGetById(transaction, snapshotId, out FileSnapshotManager.Resource? snapshot, cancellationToken))
        {
          return NotFound();
        }

        fileBufferMaps.Write(transaction, storage, file, snapshot, fileId, buffer, userAuthenticationToken, cancellationToken);

        return Ok();
      }
    });
  }
}
