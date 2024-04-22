using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text;

namespace RizzziGit.EnderBytes.Web;

using Commons.Memory;

using Core;
using Resources;
using Services;

public sealed partial class WebApi
{
  [NonAction]
  public static async Task UserAuthenticationTokenMiddleWare(Server server, HttpContext context, Func<Task> next)
  {
    string? authorizationHeader = context.Request.Headers.Authorization;

    if (authorizationHeader != null)
    {
      string type;
      JObject tokenJson;

      {
        string[] a = authorizationHeader.Split(" ");

        type = a[0];
        tokenJson = JObject.Parse(CompositeBuffer.From(a[1], StringEncoding.Base64).ToString());
      }

      if (type != "Basic")
      {
        new UnauthorizedResult().ExecuteResult(new()
        {
          HttpContext = context
        });

        return;
      }

      long userId;
      string token;
      try
      {
        userId = (long)tokenJson["userId"]!;
        token = (string)tokenJson["token"]!;
      }
      catch
      {
        new UnauthorizedResult().ExecuteResult(new()
        {
          HttpContext = context
        });

        return;
      }

      if (!await server.ResourceService.Transact(async (transaction, cancellationToken) =>
      {
        UserManager.Resource? user;
        UserAuthenticationToken? userAuthenticationToken;
        if (
          ((user = await server.ResourceService.GetManager<UserManager>().GetById(transaction, userId, cancellationToken)) != null) &&
          ((userAuthenticationToken = await server.ResourceService.GetManager<UserAuthenticationManager>().GetSessionToken(transaction, user, token, cancellationToken)) != null)
        )
        {
          context.RequestServices.GetRequiredService<WebApiContext>().Token = userAuthenticationToken;

          UserAuthenticationSessionTokenManager.Resource userAuthenticationSessionTokenResource = await server.ResourceService.GetManager<UserAuthenticationSessionTokenManager>().GetByUserAuthentication(transaction, userAuthenticationToken.UserAuthentication, cancellationToken);

          await server.ResourceService.GetManager<UserAuthenticationSessionTokenManager>().ResetExpiryTime(transaction, userAuthenticationSessionTokenResource, 36000 * 1000 * 24, cancellationToken);
          return true;
        }

        return false;
      }))
      {
        new UnauthorizedResult().ExecuteResult(new()
        {
          HttpContext = context
        });

        return;
      }

    }

    await next();
  }

  public sealed record PasswordLoginRequest(string Username, string Password);
  public sealed record PasswordLoginResponse(long UserId, string Token);

  [Route("~/auth/password-login")]
  [HttpPost]
  public Task<ActionResult<PasswordLoginResponse>> Login([FromBody] PasswordLoginRequest request) => ResourceService.Transact<ActionResult<PasswordLoginResponse>>(async (transaction, cancellationToken) =>
  {
    if (TryGetUserAuthenticationToken(out _))
    {
      return Conflict();
    }

    (string Username, string Password) = request;

    UserManager.Resource? user;
    UserAuthenticationToken? userAuthenticationToken;

    if (
      ((user = await GetResourceManager<UserManager>().GetByUsername(transaction, Username, cancellationToken)) == null) ||
      ((userAuthenticationToken = await GetResourceManager<UserAuthenticationManager>().GetByPayload(transaction, user, Encoding.UTF8.GetBytes(Password), UserAuthenticationType.Password)) == null)
    )
    {
      return Unauthorized();
    }

    string token = await GetResourceManager<UserAuthenticationManager>().CreateSessionToken(transaction, user, userAuthenticationToken, cancellationToken);
    await Server.ResourceService.GetManager<UserAuthenticationManager>().TruncateSessionToken(transaction, user, cancellationToken);

    return Ok(new PasswordLoginResponse(user.Id, token));
  });

  [Route("~/auth/logout")]
  public async Task<ActionResult> Logout()
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized(401);
    }

    await ResourceService.Transact(async (transaction, cancellationToken) =>
    {
      await GetResourceManager<UserAuthenticationManager>().Delete(transaction, userAuthenticationToken.UserAuthentication, cancellationToken);
    });

    return Ok();
  }
}
