using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Services;

using System.Diagnostics.CodeAnalysis;
using Commons.Collections;
using Commons.Memory;

using Core;
using Resources;

public partial class WebApi
{
  private static readonly WeakKeyDictionary<HttpRequest, UserAuthenticationResource.UserAuthenticationToken> AuthenticationTokens = [];

  [NonAction]
  public static async Task ClearUserAuthenticationTokenMiddleWare(HttpContext context, Func<Task> func)
  {
    AuthenticationTokens.Remove(context.Request);

    await func();
  }

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
          server.ResourceService.GetManager<UserResource.ResourceManager>().TryGetById(transaction, userId, out UserResource? user, cancellationToken) &&
          server.ResourceService.GetManager<UserAuthenticationResource.ResourceManager>().TryGetSessionToken(transaction, user, token, out var userAuthenticationToken, cancellationToken)
        )
        {
          AuthenticationTokens.Add(context.Request, userAuthenticationToken);

          UserAuthenticationSessionTokenResource userAuthenticationSessionTokenResource = server.ResourceService.GetManager<UserAuthenticationSessionTokenResource.ResourceManager>().GetByUserAuthentication(transaction, userAuthenticationToken.UserAuthentication, cancellationToken);

          server.ResourceService.GetManager<UserAuthenticationSessionTokenResource.ResourceManager>().ResetExpiryTime(transaction, userAuthenticationSessionTokenResource, 36000 * 1000 * 24, cancellationToken);
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
  public bool TryGetUserAuthenticationToken([NotNullWhen(true)] out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken) => AuthenticationTokens.TryGetValue(HttpContext.Request, out userAuthenticationToken);
}
