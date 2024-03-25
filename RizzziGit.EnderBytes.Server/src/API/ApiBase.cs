using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace RizzziGit.EnderBytes.API;

using Core;
using Resources;
using Services;

[ApiController]
public abstract class ApiBase(Server server) : ControllerBase
{
  public readonly Server Server = server;
  public ResourceService ResourceService => Server.ResourceService;
  public WebService WebService => Server.WebService;

  public T GetResourceManager<T>() where T : ResourceService.ResourceManager => ResourceService.GetResourceManager<T>();

  [NonAction]
  public bool TryGetUserAuthentication(ResourceService.Transaction transaction, [NotNullWhen(true)] out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken)
  {
    userAuthenticationToken = null;

    try
    {
      string[] authorizationHeader = $"{HttpContext.Request.Headers.Authorization}".Split(' ');

      if (authorizationHeader.Length == 2 && authorizationHeader[0] == "EDCustom")
      {
        string[] tokenSplit = authorizationHeader[1].Split(".");

        _ = (tokenSplit.Length == 2) &&
          transaction.ResourceService.GetResourceManager<UserResource.ResourceManager>().TryGetById(transaction, Convert.ToInt64(tokenSplit[0]), out UserResource? user) &&
          transaction.ResourceService.GetResourceManager<UserAuthenticationResource.ResourceManager>().TryGetToken(transaction, user, tokenSplit[1], out userAuthenticationToken);
      }
    }
    catch { }

    return userAuthenticationToken != null;
  }
}
