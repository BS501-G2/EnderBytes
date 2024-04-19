using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using Services;

public sealed partial class WebApi
{
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

      _ => Ok(fileManager.ScanFolder(transaction, storage, file, userAuthenticationToken, cancellationToken).ToArrayAsync())
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
}
