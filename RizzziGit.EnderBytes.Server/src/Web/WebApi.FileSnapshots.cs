using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Core;
using Resources;

public sealed partial class WebApi
{
  // [Route("/file/!root/snapshots")]
  // [Route("/file/:{id}/snapshots")]
  // [HttpGet]
  // public async Task<ActionResult> GetFileSnapshots(long? id)
  // {
  //   ActionResult getByIdResult = await GetById(id);

  //   if (!TryGetValueFromResult(getByIdResult, out FileManager.Resource? file))
  //   {
  //     return getByIdResult;
  //   }
  //   if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
  //   {
  //     return Unauthorized();
  //   }
  // }
}
