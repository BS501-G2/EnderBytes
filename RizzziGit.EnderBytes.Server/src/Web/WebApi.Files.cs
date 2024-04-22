using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Core;
using Resources;

public sealed partial class WebApi
{
  public sealed record GetByIdRequest(long? FileId);

  public WebApiContext.InstanceHolder<FileManager.Resource> CurrentFile => new(this, nameof(CurrentFile));
  public WebApiContext.InstanceHolder<StorageManager.Resource> CurrentStorage => new(this, nameof(CurrentStorage));

  [Route("~/file/!root")]
  [Route("~/file/:{fileId}")]
  [HttpGet]
  public async Task<ActionResult> GetById(long? fileId)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    StorageManager storageManager = ResourceService.GetManager<StorageManager>();
    FileManager fileManager = ResourceService.GetManager<FileManager>();

    FileManager.Resource? file;
    StorageManager.Resource? storage;

    return await ResourceService.Transact<ActionResult>(async (transaction, cancellationToken) =>
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

      CurrentStorage.Set(storage);
      CurrentFile.Set(file);

      return Ok(file);
    });
  }

  public sealed record GetPathChainResponse(FileManager.Resource Root, FileManager.Resource[] Chain, bool IsSharePoint);

  [Route("~/file/!root/path-chain")]
  [Route("~/file/:{id}/path-chain")]
  [HttpGet]
  public async Task<ActionResult> GetPathChain(long? id)
  {
    ActionResult getByIdResult = await GetById(id);

    if (!TryGetValueFromResult(getByIdResult, out FileManager.Resource? file))
    {
      return getByIdResult;
    }
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return await ResourceService.Transact<ActionResult>(async (transaction, cancellationToken) =>
    {
      FileManager fileManager = ResourceService.GetManager<FileManager>();
      StorageManager storageManager = ResourceService.GetManager<StorageManager>();

      DecryptedKeyInfo decryptedKeyInfo = await storageManager.DecryptKey(transaction, CurrentStorage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);
      FileManager.Resource? rootFolder;
      bool isSharePoint;

      if (decryptedKeyInfo.FileAccess == null)
      {
        rootFolder = await storageManager.GetRootFolder(transaction, CurrentStorage, userAuthenticationToken, cancellationToken);
        isSharePoint = false;
      }
      else
      {
        if ((rootFolder = await fileManager.GetById(transaction, decryptedKeyInfo.FileAccess.TargetFileId, cancellationToken)) == null)
        {
          return Forbid();
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

      return Ok(new GetPathChainResponse(rootFolder, [.. tree], isSharePoint));
    });
  }

  [Route("~/file/!root/files")]
  [Route("~/file/:{id}/files")]
  [HttpGet]
  public async Task<ActionResult> ScanFolder(long? id)
  {
    ActionResult getByIdResult = await GetById(id);

    if (!TryGetValueFromResult(getByIdResult, out FileManager.Resource? file))
    {
      return getByIdResult;
    }
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    if (file.Type != FileType.Folder)
    {
      return BadRequest();
    }

    FileManager fileManager = ResourceService.GetManager<FileManager>();

    return await ResourceService.Transact<ActionResult>(async (transaction, cancellationToken) =>
    {
      return Ok(await fileManager.ScanFolder(transaction, CurrentStorage, file, userAuthenticationToken, cancellationToken).ToArrayAsync(cancellationToken));
    });
  }

  public sealed record CreateFileRequest(bool IsFile, string Name);

  [Route("~/file/!root/files")]
  [Route("~/file/:{id}/files")]
  [HttpPost]
  public async Task<ActionResult> CreateFile(long? id, [FromBody] CreateFileRequest request)
  {
    ActionResult getByIdResult = await GetById(id);
    if (!TryGetValueFromResult(getByIdResult, out FileManager.Resource? file))
    {
      return getByIdResult;
    }

    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return await ResourceService.Transact<ActionResult>(async (transaction, cancellationToken) =>
    {
      FileManager fileManager = ResourceService.GetManager<FileManager>();

      FileManager.Resource newFile = request.IsFile
        ? await fileManager.CreateFile(transaction, CurrentStorage, file, request.Name, userAuthenticationToken, cancellationToken)
        : await fileManager.CreateFolder(transaction, CurrentStorage, file, request.Name, userAuthenticationToken, cancellationToken);

      return Ok(newFile);
    });
  }
}
