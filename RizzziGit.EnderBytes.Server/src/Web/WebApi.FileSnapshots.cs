using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using RizzziGit.Commons.Memory;
using RizzziGit.EnderBytes.Services;
using Utilities;

public sealed partial class WebApi
{
  [Route("/file/!root/snapshots")]
  [Route("/file/:{id}/snapshots")]
  [HttpGet]
  public Task<ActionResult> GetFileSnapshots(long? id) => Wrap(async () =>
  {
    ActionResult getByIdResult = await GetFileById(id);

    if (!TryGetValueFromResult(getByIdResult, out FileManager.Resource? file))
    {
      return getByIdResult;
    }

    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    StorageManager.Resource storage = CurrentStorage;

    return await ResourceService.Transact<ActionResult>(async (transaction, cancellationToken) =>
    {
      return Ok(await transaction.GetManager<FileSnapshotManager>().List(transaction, storage, file, userAuthenticationToken, null, null, cancellationToken).ToArrayAsync(cancellationToken));
    });
  });

  public sealed record CreateFileSnapshotRequest(long? BaseSnapshotId);

  [Route("/file/:{id}/snapshots")]
  [HttpPost]
  public Task<ActionResult> CreateFileSnapshot(long id, [FromBody] CreateFileSnapshotRequest request) => Wrap(async () =>
  {
    ActionResult getByIdResult = await GetFileById(id);

    if (!TryGetValueFromResult(getByIdResult, out FileManager.Resource? file))
    {
      return getByIdResult;
    }

    return await ResourceService.Transact<ActionResult>(async (transaction, cancellationToken) =>
    {
      FileSnapshotManager.Resource? baseFileSnapshot = null;

      if (
        request.BaseSnapshotId != null &&
        (baseFileSnapshot = await transaction.GetManager<FileSnapshotManager>().GetById(transaction, (long)request.BaseSnapshotId, cancellationToken)) != null
      )
      {
        return NotFound();
      }

      return Ok(await transaction.GetManager<FileSnapshotManager>().Create(transaction, CurrentStorage, file, baseFileSnapshot, CurrentUserAuthenticationToken, cancellationToken));
    });
  });

  public WebApiContext.InstanceHolder<FileSnapshotManager.Resource> CurrentFileSnapshot => new(this, nameof(CurrentFileSnapshot));

  [Route("/file/{fileId}/snapshots/:{fileSnapshotId}")]
  [HttpGet]
  public Task<ActionResult> UpdateFileSnapshot(long fileId, long fileSnapshotId) => Wrap(async () =>
  {
    ActionResult getByIdResult = await GetFileById(fileId);

    if (!TryGetValueFromResult(getByIdResult, out FileManager.Resource? file))
    {
      return getByIdResult;
    }

    return await ResourceService.Transact<ActionResult>(async (transaction, cancellationToken) =>
    {
      FileSnapshotManager.Resource? fileSnapshot;
      if ((fileSnapshot = await transaction.GetManager<FileSnapshotManager>().GetById(transaction, fileSnapshotId, cancellationToken)) == null)
      {
        return NotFound();
      }

      return Ok(CurrentFileSnapshot.Set(fileSnapshot));
    });
  });

  [Route("/file/{fileId}/snapshots/:{fileSnapshotId}/content")]
  [HttpGet]
  public Task<ActionResult> GetFileSnapshotContent(long fileId, long fileSnapshotId) => Wrap(async () =>
  {
    ActionResult getFileResult = await GetFileById(fileId);
    if (!TryGetValueFromResult(getFileResult, out FileManager.Resource? file))
    {
      return getFileResult;
    }

    ActionResult getFileSnapshotResult = await UpdateFileSnapshot(fileId, fileSnapshotId);
    if (!TryGetValueFromResult(getFileSnapshotResult, out FileSnapshotManager.Resource? fileSnapshot))
    {
      return getFileSnapshotResult;
    }

    return File(new FileSnapshotContentReadStream(ResourceService, CurrentStorage, file, fileSnapshot, CurrentUserAuthenticationToken), "application/octet-stream", file.Name, true);
  });

  [Route("/file/{fileId}/snapshots/:{fileSnapshotId}/content")]
  [HttpPost]
  public Task<ActionResult> UploadFileSnapshotContent(long fileId, long fileSnapshotId) => Wrap(async () =>
  {
    
  });

  public sealed class FileSnapshotContentReadStream(ResourceService service, StorageManager.Resource storage, FileManager.Resource file, FileSnapshotManager.Resource fileSnapshot, UserAuthenticationToken userAuthenticationToken) : Stream
  {
    private readonly ResourceService Service = service;
    private readonly StorageManager.Resource Storage = storage;
    private readonly FileManager.Resource File = file;
    private readonly FileSnapshotManager.Resource FileSnapshot = fileSnapshot;
    private readonly UserAuthenticationToken UserAuthenticationToken = userAuthenticationToken;

    public override bool CanRead => true;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length
    {
      get
      {
        return runAsync().WaitSync();

        Task<long> runAsync() => Service.Transact(async (transaction, cancellationToken) =>
        {
          return await transaction.GetManager<FileBufferMapManager>().GetSize(transaction, Storage, File, FileSnapshot, cancellationToken);
        });
      }
    }

    private long PositionBackingField = 0;
    public override long Position
    {
      get => PositionBackingField;
      set
      {
        ArgumentOutOfRangeException.ThrowIfLessThan(Length, value, nameof(value));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(0, value, nameof(value));

        PositionBackingField = value;
      }
    }

    public override void Flush() { }
    public override int Read(byte[] buffer, int offset, int count)
    {
      CompositeBuffer result = Task.Run(runAsync).WaitSync();
      result.Write(0, buffer, offset, count);
      return (int)result.Length;

      Task<CompositeBuffer> runAsync() => Service.Transact(async (transaction, cancellationToken) =>
      {
        return await transaction.GetManager<FileBufferMapManager>().Read(transaction, Storage, File, FileSnapshot, Position, count, UserAuthenticationToken, cancellationToken);
      });
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      if (origin == SeekOrigin.End)
      {
        offset = Length - offset;
      }
      else if (origin == SeekOrigin.Current)
      {
        offset += Position;
      }

      ArgumentOutOfRangeException.ThrowIfLessThan(Length, offset, nameof(offset));
      ArgumentOutOfRangeException.ThrowIfGreaterThan(0, offset, nameof(offset));

      return PositionBackingField = offset;
    }

    public override void SetLength(long value) => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
    {
      Task.Run(runAsync).WaitSync();
      Task runAsync() => Service.Transact(async (transaction, cancellationToken) =>
      {
        await transaction.GetManager<FileBufferMapManager>().Write(transaction, Storage, File, FileSnapshot, offset, CompositeBuffer.From(buffer, offset, count), UserAuthenticationToken, cancellationToken);
      });
    }
  }
}
