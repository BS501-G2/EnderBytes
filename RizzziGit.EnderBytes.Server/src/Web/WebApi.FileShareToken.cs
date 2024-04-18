using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;

public sealed partial class WebApi
{
  [Route("/file/:{id}/shares")]
  [Route("/file/!root/shares")]
  [HttpGet]
  public async Task<ActionResult<FileAccessManager.Resource[]>> GetFileShareToken(long? id)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return await ResourceService.Transact<ActionResult<FileAccessManager.Resource[]>>((transaction, cancellationToken) =>
    {
      if (id == null)
      {
        return Ok(Array.Empty<FileAccessManager.Resource>());
      }

      if (
        !GetResourceManager<FileManager>().TryGetById(transaction, (long)id, out FileManager.Resource? file, cancellationToken) ||
        !GetResourceManager<StorageManager>().TryGetById(transaction, file.StorageId, out StorageManager.Resource? storage, cancellationToken)
      )
      {
        return NotFound();
      }

      return Ok(GetResourceManager<FileAccessManager>().List(transaction, storage, file, userAuthenticationToken, null, null, cancellationToken));
    });
  }
}
