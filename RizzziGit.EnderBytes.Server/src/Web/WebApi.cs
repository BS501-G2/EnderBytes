using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Core;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Resources;
using Services;

[ApiController]
[RequestSizeLimit(1024 * 1024 * 256)]
[Route("/[controller]")]
public sealed partial class WebApi(WebApiContext context, Server server) : ControllerBase
{
  public override ObjectResult Problem(string? detail = null, string? instance = null, int? statusCode = null, string? title = null, string? type = null)
  {
    ProblemDetails problemDetails = ProblemDetailsFactory != null
      ? ProblemDetailsFactory.CreateProblemDetails(
          HttpContext,
          statusCode,
          title,
          type,
          detail,
          instance
        )
      : new ValidationProblemDetails()
      {
        Detail = detail,
        Instance = instance,
        Status = statusCode,
        Title = title,
        Type = type,
      };

    return StatusCode(500, Error(500, problemDetails));
  }

  public override ActionResult ValidationProblem(string? detail = null, string? instance = null, int? statusCode = null, string? title = null, string? type = null, [ActionResultObjectValue] ModelStateDictionary? modelStateDictionary = null)
  {
    ValidationProblemDetails validationProblemDetails = ProblemDetailsFactory != null
      ? ProblemDetailsFactory.CreateValidationProblemDetails(
          HttpContext,
          modelStateDictionary ?? ModelState,
          statusCode,
          title,
          type,
          detail,
          instance
        )
      : new ValidationProblemDetails(modelStateDictionary ?? ModelState)
      {
        Detail = detail,
        Instance = instance,
        Status = statusCode,
        Title = title,
        Type = type,
      };

    return StatusCode(400, Error(400, validationProblemDetails));
  }

  public readonly WebApiContext ApiContext = context;
  public readonly Server Server = server;
  public ResourceService ResourceService => Server.ResourceService;

  [NonAction]
  public T GetResourceManager<T>() where T : ResourceService.ResourceManager => Server.ResourceService.GetManager<T>();

  public sealed record HeartBeatResponse(bool AdminCreationNeeded);

  [Route("~/")]
  [HttpGet]
  public Result HeartBeat()
  {
    return Data(new HeartBeatResponse(false));
  }

  public WebApiContext.InstanceHolder<UserAuthenticationToken> CurrentUserAuthenticationToken => new(this, nameof(CurrentUserAuthenticationToken));

  [NonAction]
  public bool TryGetUserAuthenticationToken([NotNullWhen(true)] out UserAuthenticationToken? userAuthenticationToken) => (userAuthenticationToken = HttpContext.RequestServices.GetRequiredService<WebApiContext>().Token) != null;

  [NonAction]
  public bool TryGetValueFromResult<T>(ObjectResult result, [NotNullWhen(true)] out T? value) where T : class
  {
    if (result.Value is T value2)
    {
      value = value2;

      return true;
    }

    if (result.Value is Result apiResult && apiResult.TryGetValueFromResult(out value))
    {
      return true;
    }

    value = default;
    return false;
  }

  private long RunFlag = 0;

  [NonAction]
  public async Task<ObjectResult> Run(Func<Task<Result>> action)
  {
    Result result = await run();
    return StatusCode(result.Status, result);

    async Task<Result> run()
    {
      try
      {
        if (RunFlag == 0)
        {
          if (TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
          {
            CurrentUserAuthenticationToken.Set(userAuthenticationToken);
          }
        }

        RunFlag++;
        try
        {
          return await action();
        }
        catch (Exception exception)
        {
          return exception switch
          {
            ResourceService.ResourceManager.NotFoundException => Error(404, exception),

            ResourceService.ResourceManager.ConstraintException or
            FileManager.NotAFileException or
            FileManager.NotAFolderException or
            FileManager.NotSupportedException or
            FileManager.InvalidFileTypeException or
            FileManager.FileTreeException or
            FileManager.FileDontBelongToStorageException or
            ResourceService.ResourceManager.NoMatchException => Error(400, exception),

            StorageManager.AccessDeniedException or
            StorageManager.StorageEncryptDeniedException or
            StorageManager.StorageDecryptDeniedException => Error(400, exception),

            _ => Error(exception),
          };
        }
      }
      finally
      {
        RunFlag--;
      }
    }
  }
}
