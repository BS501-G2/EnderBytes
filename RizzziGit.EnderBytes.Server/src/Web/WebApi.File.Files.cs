using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using Services;

public sealed partial class WebApi
{
  public sealed record GetPathChainResponse(FileManager.Resource Root, FileManager.Resource[] Chain, bool IsSharePoint);

  [Route("/file/!root/path-chain")]
  [Route("/file/:{id}/path-chain")]
  [HttpGet]
  public Task<ActionResult> GetPathChain(long? id) => HandleFileRoute(new Route_File.Id.PathChain(id));

  [Route("/file/!root/files")]
  [Route("/file/:{id}/files")]
  [HttpGet]
  public Task<ActionResult> ScanFolder(long? id) => HandleFileRoute(new Route_File.Id.Files(id));

  public sealed record CreateFileRequest(bool IsFile, string Name);

  [Route("/file/!root/files")]
  [Route("/file/:{id}/files")]
  [HttpPost]
  public Task<ActionResult> CreateFile(long? id, [FromBody] CreateFileRequest request) => HandleFileRoute(new Route_File.Id.Files.Create(id, request.IsFile, request.Name));

  [NonAction]
  public async Task<ActionResult> HandleFileIdFilesRoute(Route_File.Id.Files request, ResourceService.Transaction transaction, FileManager.Resource file, StorageManager.Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken)
  {
    if (file.Type != FileType.Folder)
    {
      return BadRequest();
    }

    FileManager fileManager = ResourceService.GetManager<FileManager>();

    return request switch
    {
      Route_File.Id.Files.Create fileIdFilesCreateRequest => await HandleFileIdFilesCreateRoute(fileIdFilesCreateRequest, transaction, file, storage, userAuthenticationToken, cancellationToken),

      _ => Ok(await fileManager.ScanFolder(transaction, storage, file, userAuthenticationToken, cancellationToken).ToArrayAsync(cancellationToken))
    };
  }

  [NonAction]
  public async Task<ActionResult> HandleFileIdFilesCreateRoute(Route_File.Id.Files.Create request, ResourceService.Transaction transaction, FileManager.Resource file, StorageManager.Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken)
  {
    FileManager fileManager = ResourceService.GetManager<FileManager>();

    FileManager.Resource newFile = request.IsFile
      ? await fileManager.CreateFile(transaction, storage, file, request.Name, userAuthenticationToken, cancellationToken)
      : await fileManager.CreateFolder(transaction, storage, file, request.Name, userAuthenticationToken, cancellationToken);

    return Ok(newFile);
  }

  [NonAction]
  public async Task<ActionResult> HandleFileIdPathChainRoute(Route_File.Id.PathChain request, ResourceService.Transaction transaction, FileManager.Resource file, StorageManager.Resource storage, UserAuthenticationToken userAuthenticationToken, CancellationToken cancellationToken = default)
  {
    FileManager fileManager = ResourceService.GetManager<FileManager>();
    StorageManager storageManager = ResourceService.GetManager<StorageManager>();

    DecryptedKeyInfo decryptedKeyInfo = await storageManager.DecryptKey(transaction, storage, file, userAuthenticationToken, FileAccessType.Read, cancellationToken);
    FileManager.Resource? rootFolder;
    bool isSharePoint;

    if (decryptedKeyInfo.FileAccess == null)
    {
      rootFolder = await storageManager.GetRootFolder(transaction, storage, userAuthenticationToken, cancellationToken);
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
  }
}
