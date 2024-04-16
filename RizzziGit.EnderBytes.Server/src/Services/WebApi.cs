using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Services;

using System.Diagnostics.CodeAnalysis;
using System.Resources;
using System.Text;
using Core;
using Resources;

[ApiController]
public partial class WebApi(Server server) : Controller, IDisposable
{
  public readonly Server Server = server;
  public ResourceService ResourceService => Server.ResourceService;
  [NonAction]
  public T GetResourceManager<T>() where T : ResourceService.ResourceManager => Server.ResourceService.GetManager<T>();

  public sealed record PasswordLoginRequest(string Username, string Password);
  public sealed record PasswordLoginResponse(long UserId, string Token);

  [Route("/auth/password-login")]
  [HttpPost]
  public Task<ActionResult<PasswordLoginResponse>> Login([FromBody] PasswordLoginRequest request) => ResourceService.Transact<ActionResult<PasswordLoginResponse>>((transaction, cancellationToken) =>
  {
    if (TryGetUserAuthenticationToken(out _))
    {
      return StatusCode(409);
    }

    (string Username, string Password) = request;

    if (
      !GetResourceManager<UserResource.ResourceManager>().TryGetByUsername(transaction, Username, out UserResource? user, cancellationToken) ||
      !GetResourceManager<UserAuthenticationResource.ResourceManager>().TryGetByPayload(transaction, user, Encoding.UTF8.GetBytes(Password), UserAuthenticationType.Password, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken)
    )
    {
      return StatusCode(401);
    }

    string token = GetResourceManager<UserAuthenticationResource.ResourceManager>().CreateSessionToken(transaction, user, userAuthenticationToken, cancellationToken);
    Server.ResourceService.GetManager<UserAuthenticationResource.ResourceManager>().TryTruncateSessionTokens(transaction, user, cancellationToken);

    return Ok(new PasswordLoginResponse(user.Id, token));
  });

  [Route("/auth/logout")]
  public async Task<ActionResult> Logout()
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken))
    {
      return StatusCode(401);
    }

    await ResourceService.Transact((transaction, cancellationToken) =>
    {
      GetResourceManager<UserAuthenticationResource.ResourceManager>().Delete(transaction, userAuthenticationToken.UserAuthentication, cancellationToken);
    });

    return Ok();
  }
}
