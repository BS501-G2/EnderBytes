using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Core;
using Resources;
using Services;

public sealed class MiscellaneousRequestContext
{
  public UserAuthenticationToken? Token = null;
}

[ApiController]
[RequestSizeLimit(1024 * 1024 * 256)]
public sealed partial class WebApi(Server server) : Controller, IDisposable
{
  public readonly Server Server = server;
  public ResourceService ResourceService => Server.ResourceService;

  [NonAction]
  public T GetResourceManager<T>() where T : ResourceService.ResourceManager => Server.ResourceService.GetManager<T>();

  public sealed record HeartBeatResponse(bool AdminCreationNeeded);

  [Route("/")]
  [HttpGet]
  public ActionResult<HeartBeatResponse> HeartBeat()
  {
    return Ok(new HeartBeatResponse(false));
  }
}
