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
  public sealed record AuthenticationHolder()
  {
    public UserAuthenticationToken? Token;
  };

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
        context.Response.StatusCode = 401;

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
        context.Response.StatusCode = 401;

        return;
      }

      if (!await server.ResourceService.Transact((transaction, cancellationToken) =>
      {
        if (
          server.ResourceService.GetManager<UserManager>().TryGetById(transaction, userId, out UserManager.Resource? user, cancellationToken) &&
          server.ResourceService.GetManager<UserAuthenticationManager>().TryGetSessionToken(transaction, user, token, out var userAuthenticationToken, cancellationToken)
        )
        {
          context.RequestServices.GetRequiredService<AuthenticationHolder>().Token = userAuthenticationToken;

          UserAuthenticationSessionTokenManager.Resource userAuthenticationSessionTokenResource = server.ResourceService.GetManager<UserAuthenticationSessionTokenManager>().GetByUserAuthentication(transaction, userAuthenticationToken.UserAuthentication, cancellationToken);

          server.ResourceService.GetManager<UserAuthenticationSessionTokenManager>().ResetExpiryTime(transaction, userAuthenticationSessionTokenResource, 36000 * 1000 * 24, cancellationToken);
          return true;
        }

        return false;
      }))
      {
        context.Response.StatusCode = 401;

        return;
      }

    }

    await next();
  }

  [NonAction]
  public bool TryGetUserAuthenticationToken([NotNullWhen(true)] out UserAuthenticationToken? userAuthenticationToken)
  {
    return (userAuthenticationToken = HttpContext.RequestServices.GetRequiredService<AuthenticationHolder>().Token) != null;
  }
}
