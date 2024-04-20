using Microsoft.AspNetCore.Mvc;
using RizzziGit.EnderBytes.Resources;

namespace RizzziGit.EnderBytes.Web;

public sealed partial class WebApi
{
  [Route("/storage")]
  [HttpGet]
  public async Task<ActionResult> GetStorage()
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return Ok(await ResourceService.Transact((transaction, cancellationToken) => GetResourceManager<StorageManager>().GetByOwnerUser(transaction, userAuthenticationToken, cancellationToken)));
  }
}
