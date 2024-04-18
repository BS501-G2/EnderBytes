using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;

public sealed partial class WebApi
{
  [Route("/file/:{id}")]
  [HttpGet]
  public async Task<ActionResult<FileManager.Resource>> GetFile(long id)
  {
    FileManager files = GetResourceManager<FileManager>();
    StorageManager storages = GetResourceManager<StorageManager>();

    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return await ResourceService.Transact<ActionResult<FileManager.Resource>>((transaction, cancellationToken) =>
    {
      if (
        !files.TryGetById(transaction, id, out FileManager.Resource? file, cancellationToken) ||
        !storages.TryGetById(transaction, file.StorageId, out StorageManager.Resource? storage, cancellationToken)
      )
      {
        return NotFound();
      }

      DecryptedKeyInfo decryptedKey = storages.DecryptKey(transaction, storage, file, userAuthenticationToken, cancellationToken: cancellationToken);
      if (decryptedKey.Key == null)
      {
        FileManager.Resource rootFolder = storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);

        if (!files.IsEqualToOrInsideOf(transaction, storage, rootFolder, file, cancellationToken))
        {
          return Forbid();
        }
      }

      return Ok(file);
    });
  }

  [Route("/file/!root")]
  [HttpGet]
  public async Task<ActionResult<FileManager.Resource>> GetRootFolder()
  {
    FileManager files = GetResourceManager<FileManager>();
    StorageManager storages = GetResourceManager<StorageManager>();

    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return await ResourceService.Transact<ActionResult<FileManager.Resource>>((transaction, cancellationToken) =>
    {
      StorageManager.Resource storage = storages.GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);

      return Ok(storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken));
    });
  }

  public sealed record CreateFileRequest(bool IsFile, string Name);

  [Route("/file/:{id}")]
  [HttpPost]
  public async Task<ActionResult<FileManager.Resource>> CreateFile(long id, [FromBody] CreateFileRequest request)
  {
    FileManager files = GetResourceManager<FileManager>();
    StorageManager storages = GetResourceManager<StorageManager>();

    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return await ResourceService.Transact<ActionResult<FileManager.Resource>>((transaction, cancellationToken) =>
    {
      if (
        !files.TryGetById(transaction, id, out FileManager.Resource? parentFolder, cancellationToken) ||
        !storages.TryGetById(transaction, parentFolder.StorageId, out StorageManager.Resource? storage, cancellationToken)
      )
      {
        return Unauthorized();
      }

      DecryptedKeyInfo decryptedKey = storages.DecryptKey(transaction, storage, parentFolder, userAuthenticationToken, cancellationToken: cancellationToken);
      if (decryptedKey.Key == null)
      {
        FileManager.Resource rootFolder = storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);

        if (!files.IsEqualToOrInsideOf(transaction, storage, rootFolder, parentFolder, cancellationToken))
        {
          return Forbid();
        }
      }

      if (parentFolder.Type != FileType.Folder)
      {
        return BadRequest();
      }

      FileManager.Resource newFile = request.IsFile
        ? files.CreateFile(transaction, storage, parentFolder, request.Name, userAuthenticationToken, cancellationToken)
        : files.CreateFolder(transaction, storage, parentFolder, request.Name, userAuthenticationToken, cancellationToken);

      return Ok(newFile);
    });
  }

  [Route("/file/!root")]
  [HttpPost]
  public async Task<ActionResult<FileManager.Resource>> CreateFile([FromBody] CreateFileRequest request)
  {
    FileManager files = GetResourceManager<FileManager>();
    StorageManager storages = GetResourceManager<StorageManager>();

    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return await ResourceService.Transact<ActionResult<FileManager.Resource>>((transaction, cancellationToken) =>
    {
      StorageManager.Resource storage = storages.GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);
      FileManager.Resource file = storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);

      FileManager.Resource newFile = request.IsFile
        ? files.CreateFile(transaction, storage, file, request.Name, userAuthenticationToken, cancellationToken)
        : files.CreateFolder(transaction, storage, file, request.Name, userAuthenticationToken, cancellationToken);

      return Ok(newFile);
    });
  }

  [Route("/file/!root/files")]
  [Route("/file/:{id}/files")]
  [HttpGet]
  public async Task<ActionResult<FileManager.Resource[]>> ScanFolder(long? id)
  {
    FileManager files = GetResourceManager<FileManager>();
    StorageManager storages = GetResourceManager<StorageManager>();

    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return await ResourceService.Transact<ActionResult<FileManager.Resource[]>>((transaction, cancellationToken) =>
    {
      if (id == null)
      {
        StorageManager.Resource storage = storages.GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken);
        FileManager.Resource file = storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);

        return Ok(files.ScanFolder(transaction, storage, file, userAuthenticationToken, cancellationToken).ToArray());
      }
      else
      {
        if (
          !files.TryGetById(transaction, (long)id, out FileManager.Resource? file, cancellationToken) ||
          !storages.TryGetById(transaction, file.StorageId, out StorageManager.Resource? storage, cancellationToken)
        )
        {
          return NotFound();
        }

        DecryptedKeyInfo decryptedKey = storages.DecryptKey(transaction, storage, file, userAuthenticationToken, cancellationToken: cancellationToken);
        if (decryptedKey.Key == null)
        {
          FileManager.Resource rootFolder = storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);
          if (!files.IsEqualToOrInsideOf(transaction, storage, rootFolder, file, cancellationToken))
          {
            return Forbid();
          }
        }

        return Ok(files.ScanFolder(transaction, storage, file, userAuthenticationToken, cancellationToken).ToArray());
      }
    });
  }

  // [Route("/file/:{id}/path-chain")]
  // [Route("/file/!root/path-chain")]
  // [HttpGet]
  // public async Task<ActionResult<FileManager.Resource[]>> GetPathChain(long? id)
  // {
  //   if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
  //   {
  //     return Unauthorized();
  //   }

  //   FileManager files = GetResourceManager<FileManager>();
  //   StorageManager storages = GetResourceManager<StorageManager>();
  //   FileAccessManager fileAccesses = GetResourceManager<FileAccessManager>();

  //   return await ResourceService.Transact<ActionResult<FileManager.Resource[]>>((transaction, cancellationToken) =>
  //   {
  //     if (id == null)
  //     {
  //       return Ok(Array.Empty<FileManager.Resource>());
  //     }

  //     List<FileManager.Resource> chain = [];
  //     if (
  //       !files.TryGetById(transaction, (long)id, out FileManager.Resource? file, cancellationToken) ||
  //       !storages.TryGetById(transaction, file.StorageId, out StorageManager.Resource? storage, cancellationToken)
  //     )
  //     {
  //       return NotFound();
  //     }

  //     DecryptedKeyInfo decryptedKeyInfo = storages.DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);

  //     if (decryptedKeyInfo.FileAccess == null)
  //     {
  //       FileManager.Resource rootFolder = storages.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);

  //       if (!files.IsEqualToOrInsideOf(transaction, storage, rootFolder, file, cancellationToken))
  //       {
  //         return Forbid();
  //       }

  //       chain.Add(file);
  //     }

  //     return Ok(chain);
  //   });
  // }
}
