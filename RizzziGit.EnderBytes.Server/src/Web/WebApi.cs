using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace RizzziGit.EnderBytes.Web;

using Core;
using Resources;
using Services;

[ApiController]
[RequestSizeLimit(1024 * 1024 * 512)]
[RequestFormLimits(
    ValueLengthLimit = 1024 * 1024 * 512,
    MultipartBodyLengthLimit = 1024 * 1024 * 512
)]
[Route("/[controller]")]
public sealed partial class WebApi(WebApiContext context, Server server) : ControllerBase
{
    public override ObjectResult Problem(
        string? detail = null,
        string? instance = null,
        int? statusCode = null,
        string? title = null,
        string? type = null
    )
    {
        ProblemDetails problemDetails =
            ProblemDetailsFactory != null
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

        return StatusCode(statusCode ?? 500, Error((ushort)(statusCode ?? 500), problemDetails));
    }

    public override ActionResult ValidationProblem(
        string? detail = null,
        string? instance = null,
        int? statusCode = null,
        string? title = null,
        string? type = null,
        [ActionResultObjectValue] ModelStateDictionary? modelStateDictionary = null
    )
    {
        ValidationProblemDetails validationProblemDetails =
            ProblemDetailsFactory != null
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

        return StatusCode(
            statusCode ?? 500,
            Error((ushort)(statusCode ?? 500), validationProblemDetails)
        );
    }

    public readonly WebApiContext ApiContext = context;
    public readonly Server Server = server;
    public ResourceService ResourceService => Server.ResourceService;

    [NonAction]
    public T GetResourceManager<T>()
        where T : ResourceService.ResourceManager => Server.ResourceService.GetManager<T>();

    public sealed record HeartBeatResponse(bool AdminCreationNeeded);

    [Route("~/")]
    [HttpGet]
    public Result HeartBeat()
    {
        return Data(new HeartBeatResponse(false));
    }

    public WebApiContext.InstanceHolder<UserAuthenticationToken> CurrentUserAuthenticationToken =>
        new(this, nameof(CurrentUserAuthenticationToken));
    public WebApiContext.InstanceHolder<ResourceService.Transaction> CurrentTransaction =>
        new(this, nameof(CurrentTransaction));

    [NonAction]
    public bool TryGetUserAuthenticationToken(
        [NotNullWhen(true)] out UserAuthenticationToken? userAuthenticationToken
    ) =>
        (
            userAuthenticationToken = HttpContext
                .RequestServices.GetRequiredService<WebApiContext>()
                .Token
        ) != null;

    [NonAction]
    public bool TryGetValueFromResult<T>(ActionResult result, [NotNullWhen(true)] out T? value)
        where T : class
    {
        if (result is ObjectResult objectResult)
        {
            if (objectResult.Value is T value2)
            {
                value = value2;

                return true;
            }

            if (
                objectResult.Value is Result apiResult
                && apiResult.TryGetValueFromResult(out value)
            )
            {
                return true;
            }
        }

        value = default;
        return false;
    }

    [NonAction]
    public async Task<ObjectResult> Run(Func<Task<ObjectResult>> action)
    {
        RunFlag++;
        try
        {
            if (TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
            {
                CurrentUserAuthenticationToken.Set(userAuthenticationToken);
            }

            try
            {
                return await action();
            }
            catch (Exception exception)
            {
                Result result = SerializeError(exception);

                return StatusCode(result.Status, result);
            }
        }
        finally
        {
            RunFlag--;
        }
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
                    if (
                        TryGetUserAuthenticationToken(
                            out UserAuthenticationToken? userAuthenticationToken
                        )
                    )
                    {
                        CurrentUserAuthenticationToken.Set(userAuthenticationToken);
                    }
                }

                RunFlag++;
                try
                {
                    if (CurrentTransaction.Optional() == null)
                    {
                        return await ResourceService.Transact(
                            async (transaction) =>
                            {
                                CurrentTransaction.Set(transaction);

                                try
                                {
                                    return await action();
                                }
                                finally
                                {
                                    CurrentTransaction.Clear();
                                }
                            }
                        );
                    }
                    else
                    {
                        return await action();
                    }
                }
                catch (Exception exception)
                {
                    return SerializeError(exception);
                }
            }
            finally
            {
                RunFlag--;
            }
        }
    }

    [NonAction]
    private ErrorResult<SerializableErrorDetails> SerializeError(Exception exception) =>
        exception switch
        {
            ResourceService.ResourceManager.NotFoundException => Error(404, exception),

            ResourceService.ResourceManager.ConstraintException
            or ResourceService.ResourceManager.NoMatchException
                => Error(400, exception),

            FileManager.InvalidAccessException => Error(403, exception),

            _ => Error(exception),
        };
}
