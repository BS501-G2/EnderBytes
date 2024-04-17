using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;

public sealed partial class WebApi
{
  [Route("/file/:{id}")]
  [HttpGet]
  public async Task<ActionResult> GetFile(long id)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return StatusCode(401);
    }
  }
}
