using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.API;

using System.Text;
using Core;
using Resources;

[Route("/auth/[controller]")]
public sealed class AuthenticationApi(Server server) : ApiBase(server)
{
  public sealed record GetTokenPasswordCredentials(string Username, string Password);
  public sealed record Registration(string Username, string DisplayName, string Password);

  private UserAuthenticationResource.ResourceManager UserAuthentications => ResourceService.GetResourceManager<UserAuthenticationResource.ResourceManager>();
  private UserResource.ResourceManager Users => ResourceService.GetResourceManager<UserResource.ResourceManager>();

  [HttpPut("/auth/password")]
  public async Task<IActionResult> Register([FromBody] Registration registration)
  {
    return Ok();
  }

  [HttpPost("/auth/get-token-by-password")]
  public async Task<IActionResult> GetTokenByPassword([FromBody] GetTokenPasswordCredentials credentials)
  {
    string? token = null;

    await ResourceService.Transact((transaction, cancellationToken) =>
    {
      if (
        Users.TryGetByUsername(transaction, credentials.Username, out UserResource? user, cancellationToken) &&
        UserAuthentications.TryGetByPayload(transaction, user, Encoding.UTF8.GetBytes(credentials.Password), UserAuthenticationResource.UserAuthenticationType.Password, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken)
      )
      {
        token = $"EDCustom {user.Id}.{UserAuthentications.CreateSessionToken(transaction, user, userAuthenticationToken)}";
      }
    });

    return token != null ? Ok(token) : StatusCode(400);
  }

  [HttpGet("/auth/verify")]
  public async Task<IActionResult> Get()
  {
    UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = null;

    return await ResourceService.Transact((transaction, cancellationToken) => TryGetUserAuthentication(transaction, out userAuthenticationToken))
      ? Ok(userAuthenticationToken)
      : StatusCode(400);
  }
}
