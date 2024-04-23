using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Core;
using Resources;
using Services;

[ApiController]
[RequestSizeLimit(1024 * 1024 * 256)]
[Route("/[controller]")]
public sealed partial class WebApi(WebApiContext context, Server server) : Controller, IDisposable
{
  public readonly WebApiContext ApiContext = context;
  public readonly Server Server = server;
  public ResourceService ResourceService => Server.ResourceService;

  [NonAction]
  public T GetResourceManager<T>() where T : ResourceService.ResourceManager => Server.ResourceService.GetManager<T>();

  public sealed record HeartBeatResponse(bool AdminCreationNeeded);

  [Route("~/")]
  [HttpGet]
  public ActionResult<HeartBeatResponse> HeartBeat()
  {
    return Ok(new HeartBeatResponse(false));
  }

  public WebApiContext.InstanceHolder<UserAuthenticationToken> CurrentUserAuthenticationToken => new(this, nameof(CurrentUserAuthenticationToken));

  [NonAction]
  public bool TryGetUserAuthenticationToken([NotNullWhen(true)] out UserAuthenticationToken? userAuthenticationToken)
  {
    if ((userAuthenticationToken = HttpContext.RequestServices.GetRequiredService<WebApiContext>().Token) != null)  {
      CurrentUserAuthenticationToken.Set(userAuthenticationToken);

      return true;
    }

    return false;
  }

  [NonAction]
  public bool TryGetValueFromResult<T>(ActionResult<T> result, [NotNullWhen(true)] out T? value)
  {
    if (result.Result is OkObjectResult okObjectResult)
    {
      value = (T)okObjectResult.Value!;
      return true;
    }

    value = default;
    return false;
  }

  [NonAction]
  public async Task<ActionResult> Wrap(Func<Task<ActionResult>> action)
  {
    try
    {
      return await action();
    }
    catch (ResourceService.ResourceManager.Exception exception)
    {
      return exception switch
      {
        ResourceService.ResourceManager.NotFoundException => NotFound(),

        ResourceService.ResourceManager.ConstraintException or
        FileManager.NotAFileException or
        FileManager.NotAFolderException or
        FileManager.NotSupportedException or
        FileManager.InvalidFileTypeException or
        FileManager.FileTreeException or
        FileManager.FileDontBelongToStorageException or
        ResourceService.ResourceManager.NoMatchException => BadRequest(),

        StorageManager.AccessDeniedException or
        StorageManager.StorageEncryptDeniedException or
        StorageManager.StorageDecryptDeniedException => Forbid(),

        _ => BadRequest(exception.Message),
      };
    }
  }
}
