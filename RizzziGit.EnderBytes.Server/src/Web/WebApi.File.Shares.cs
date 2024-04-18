using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using Services;

public sealed partial class WebApi
{
  [Route("/file/!root/share")]
  [Route("/file/:{id}/share")]
  [HttpGet]
  public Task<ActionResult> GetFileShares(long? id) => HandleFileRoute(new Route_File.Id.Share(id));
}
