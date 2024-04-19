using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace RizzziGit.EnderBytes.Web;

using Commons.Collections;
using Commons.Memory;

using Core;
using Resources;

public partial class WebApi
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
          context.RequestServices.GetRequiredService<MiscellaneousRequestContext>().Token = userAuthenticationToken;

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

  [NonAction]
  public bool TryGetUserAuthenticationToken([NotNullWhen(true)] out UserAuthenticationToken? userAuthenticationToken)
  {
    return (userAuthenticationToken = HttpContext.RequestServices.GetRequiredService<MiscellaneousRequestContext>().Token) != null;
  }
}
