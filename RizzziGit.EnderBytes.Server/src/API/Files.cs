using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.API;

using Core;
using Resources;

[Route("/files/[controller]")]
public sealed class FilesApi(Server server) : ApiBase(server)
{
  FileResource.ResourceManager Files => ResourceService.GetResourceManager<FileResource.ResourceManager>();
  FileSnapshotResource.ResourceManager Snapshots => ResourceService.GetResourceManager<FileSnapshotResource.ResourceManager>();
  StorageResource.ResourceManager Storages => ResourceService.GetResourceManager<StorageResource.ResourceManager>();

  public sealed record CreateFileBody(long ParentFileId, string Name, FileResource.FileType Type);

  [HttpPost("/files")]
  public Task<IActionResult> CreateFile([FromBody] CreateFileBody createFileBody) => ResourceService.Transact((transaction, cancellationToken) =>
  {
    TryGetUserAuthentication(transaction, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken);

    try
    {
      if (Files.TryGetById(transaction, createFileBody.ParentFileId, out FileResource? parentFolderResource, cancellationToken))
      {
        if (parentFolderResource.Type != FileResource.FileType.Folder)
        {
          return Error(status: 400);
        }

        if (Storages.TryGetById(transaction, parentFolderResource.StorageId, out StorageResource? storage, cancellationToken))
        {
          _ = Storages.DecryptKey(transaction, storage, parentFolderResource, userAuthenticationToken, FileAccessResource.FileAccessType.Read, cancellationToken);

          FileResource file = Files.CreateFile(transaction, storage, parentFolderResource, createFileBody.Name, userAuthenticationToken, cancellationToken);

          return Ok(file.Id);
        }

        return Error(status: 404);
      }

      return Error(status: 404);
    }
    catch { }

    return Error(status: 403);
  });

  [HttpGet("/files/{id}")]
  public Task<IActionResult> GetFileMetadata(long id) => ResourceService.Transact((transaction, cancellationToken) =>
  {
    TryGetUserAuthentication(transaction, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken);

    try
    {
      if (
        Files.TryGetById(transaction, id, out FileResource? file, cancellationToken) &&
        Storages.TryGetById(transaction, file.StorageId, out StorageResource? storage, cancellationToken)
      )
      {
        _ = Storages.DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessResource.FileAccessType.Read, cancellationToken);

        return Ok(file);
      }

      return Error(status: 404);
    }
    catch { }

    return Error(status: 403);
  });

  [HttpGet("/files/{id}/scan")]
  public Task<IActionResult> GetFolderScan(long id) => ResourceService.Transact((transaction, cancellationToken) =>
  {
    TryGetUserAuthentication(transaction, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken);

    try
    {
      if (
        Files.TryGetById(transaction, id, out FileResource? file, cancellationToken) &&
        Storages.TryGetById(transaction, file.StorageId, out StorageResource? storage, cancellationToken)
      )
      {
        if (file.Type != FileResource.FileType.Folder)
        {
          return Error(status: 400);
        }

        return Ok(Files.ScanFolder(transaction, storage, file, userAuthenticationToken, cancellationToken).Select((entry) => entry.Id));
      }

      return Error(status: 404);
    }
    catch { }

    return Error(status: 403);
  });

  [HttpGet("/files/{id}/snapshots")]
  public Task<IActionResult> GetFileSnapshots(long id) => ResourceService.Transact((transaction, cancellationToken) =>
  {
    TryGetUserAuthentication(transaction, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken);

    try
    {
      if (
        Files.TryGetById(transaction, id, out FileResource? fileResource, cancellationToken) &&
        Storages.TryGetById(transaction, fileResource.StorageId, out StorageResource? storage, cancellationToken)
      )
      {
        if (fileResource.Type != FileResource.FileType.File)
        {
          return Error(status: 400);
        }

        return Ok(Snapshots.List(transaction, storage, fileResource, cancellationToken: cancellationToken).Select((entry) => entry.Id));
      }

      return Error(status: 404);
    }
    catch { }

    return Error(status: 403);
  });

  [HttpGet("/files/{fileId}/snapshots/{snapshotId}")]
  public Task<IActionResult> GetFileSnapshot(long fileId, long snapshotId) => ResourceService.Transact((transaction, cancellationToken) =>
  {
    TryGetUserAuthentication(transaction, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken);

    try
    {
      if (
        Files.TryGetById(transaction, fileId, out FileResource? fileResource, cancellationToken) &&
        Storages.TryGetById(transaction, fileResource.StorageId, out StorageResource? storage, cancellationToken)
      )
      {
        if (fileResource.Type != FileResource.FileType.File)
        {
          return Error(status: 400);
        }

        if (Snapshots.TryGetById(transaction, snapshotId, out FileSnapshotResource? fileSnapshot, cancellationToken))
        {
          return Ok(fileSnapshot);
        }
      }

      return Error(status: 404);
    }
    catch { }

    return Error(status: 403);
  });
}
