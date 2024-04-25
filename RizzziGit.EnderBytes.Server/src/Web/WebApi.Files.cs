using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Core;
using Resources;
using RizzziGit.Commons.Memory;

public sealed partial class WebApi
{
  public sealed record GetByIdRequest(long? FileId);

  public WebApiContext.InstanceHolder<FileManager.Resource> CurrentFile => new(this, nameof(CurrentFile));
  public WebApiContext.InstanceHolder<StorageManager.Resource> CurrentStorage => new(this, nameof(CurrentStorage));

  [Route("~/file/!root")]
  [Route("~/file/:{fileId}")]
  [HttpGet]
  public Task<ObjectResult> GetFileById(long? fileId) => Run(async () =>
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Error(401);
    }

    StorageManager storageManager = ResourceService.GetManager<StorageManager>();
    FileManager fileManager = ResourceService.GetManager<FileManager>();

    FileManager.Resource? file;
    StorageManager.Resource? storage;

    return await ResourceService.Transact<Result>(async (transaction, cancellationToken) =>
    {
      if (fileId == null)
      {
        storage = await storageManager.GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);
        file = await storageManager.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);
      }
      else if (
        ((file = await fileManager.GetById(transaction, (long)fileId, cancellationToken)) == null) ||
        ((storage = await storageManager.GetById(transaction, file.StorageId, cancellationToken)) == null)
      )
      {
        return Error(404);
      }

      DecryptedKeyInfo decryptedKeyInfo = await storageManager.DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);

      if (decryptedKeyInfo.FileAccess == null)
      {
        FileManager.Resource rootFolder = await storageManager.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);

        if (!await fileManager.IsEqualToOrInsideOf(transaction, storage, rootFolder, file, cancellationToken))
        {
          return Error(403);
        }
      }

      CurrentStorage.Set(storage);
      CurrentFile.Set(file);

      return Data(file);
    });
  });

  public sealed record GetPathChainResponse(FileManager.Resource Root, FileManager.Resource[] Chain, bool IsSharePoint);

  [Route("~/file/!root/path-chain")]
  [Route("~/file/:{id}/path-chain")]
  [HttpGet]
  public async Task<ObjectResult> GetPathChain(long? id)
  {
    ObjectResult fileResult = await GetFileById(id);
    if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
    {
      return fileResult;
    }

    return await Run(async () =>
    {
      return await ResourceService.Transact<Result>(async (transaction, cancellationToken) =>
      {
        FileManager fileManager = ResourceService.GetManager<FileManager>();
        StorageManager storageManager = ResourceService.GetManager<StorageManager>();

        DecryptedKeyInfo decryptedKeyInfo = await storageManager.DecryptKey(transaction, CurrentStorage, file, CurrentUserAuthenticationToken, FileAccessType.Read, cancellationToken);
        FileManager.Resource? rootFolder;
        bool isSharePoint;

        if (decryptedKeyInfo.FileAccess == null)
        {
          rootFolder = await storageManager.GetRootFolder(transaction, CurrentStorage, CurrentUserAuthenticationToken, cancellationToken);
          isSharePoint = false;
        }
        else
        {
          if ((rootFolder = await fileManager.GetById(transaction, decryptedKeyInfo.FileAccess.TargetFileId, cancellationToken)) == null)
          {
            return Error(403);
          }

          isSharePoint = true;
        }

        List<FileManager.Resource> tree = [];

        FileManager.Resource? current = file;
        while (current?.ParentId != null && current.Id != rootFolder.Id)
        {
          tree.Insert(0, current);

          Console.WriteLine(current);

          current = await fileManager.GetById(transaction, (long)current.ParentId, cancellationToken);
        }

        return Data(new GetPathChainResponse(rootFolder, [.. tree], isSharePoint));
      });
    });
  }

  [Route("~/file/!root/files")]
  [Route("~/file/:{id}/files")]
  [HttpGet]
  public async Task<ObjectResult> ScanFolder(long? id)
  {
    ObjectResult fileResult = await GetFileById(id);
    if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
    {
      return fileResult;
    }

    return await Run(async () =>
    {
      if (file.Type != FileType.Folder)
      {
        return Error(400);
      }

      FileManager fileManager = ResourceService.GetManager<FileManager>();

      return await ResourceService.Transact<Result>(async (transaction, cancellationToken) =>
      {
        return Data(await fileManager.ScanFolder(transaction, CurrentStorage, file, CurrentUserAuthenticationToken, cancellationToken).ToArrayAsync(cancellationToken));
      });
    });
  }

  public sealed record CreateFileRequest(string Name, IFormFile Content);
  public sealed record CreateFileResponse(FileManager.Resource File, FileSnapshotManager.Resource FileSnapshot);

  [Route("~/file/!root/files/new-file")]
  [Route("~/file/:{id}/files/new-file")]
  [HttpPost]
  public async Task<ActionResult> CreateFile(long? id, [FromForm] CreateFileRequest request)
  {
    ObjectResult fileResult = await GetFileById(id);
    if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
    {
      return fileResult;
    }

    CompositeBuffer content = [];
    using Stream stream = request.Content.OpenReadStream();
    while (stream.Position < stream.Length)
    {
      long offset = stream.Position;

      {
        byte[] bytes = new byte[1024 * 32];
        int byteLength = await stream.ReadAsync(bytes);

        content.Append(bytes, 0, byteLength);
      }
    }

    return await Run(async () =>
    {
      return await ResourceService.Transact<Result>(async (transaction, cancellationToken) =>
      {
        FileManager fileManager = ResourceService.GetManager<FileManager>();
        FileBufferMapManager fileBufferMapManager = ResourceService.GetManager<FileBufferMapManager>();
        FileSnapshotManager fileSnapshotManager = ResourceService.GetManager<FileSnapshotManager>();

        FileManager.Resource newFile = await fileManager.CreateFile(transaction, CurrentStorage, file, request.Name, CurrentUserAuthenticationToken, cancellationToken);
        FileSnapshotManager.Resource fileSnapshot = await fileSnapshotManager.Create(transaction, CurrentStorage, newFile, null, CurrentUserAuthenticationToken, cancellationToken);

        await fileBufferMapManager.Write(transaction, CurrentStorage, newFile, fileSnapshot, 0, content, CurrentUserAuthenticationToken, cancellationToken);
        return Data(new CreateFileResponse(file, fileSnapshot));
      });
    });
  }

  public sealed record CreateFolderRequest(string Name);

  [Route("~/file/!root/files/new-folder")]
  [Route("~/file/:{id}/files/new-folder")]
  [HttpPost]
  public async Task<ActionResult> CreateFolder(long? id, [FromBody] CreateFolderRequest request)
  {
    ObjectResult fileResult = await GetFileById(id);
    if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
    {
      return fileResult;
    }

    return await Run(async () =>
    {
      return await ResourceService.Transact<Result>(async (transaction, cancellationToken) =>
      {
        FileManager fileManager = ResourceService.GetManager<FileManager>();

        return Data(await fileManager.CreateFolder(transaction, CurrentStorage, file, request.Name, CurrentUserAuthenticationToken, cancellationToken));
      });
    });
  }
}
