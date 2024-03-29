using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace RizzziGit.EnderBytes.API;

using System.Text;
using Core;
using Resources;

[Route("/auth/[controller]")]
public sealed class AuthenticationApi(Server server) : ApiBase(server)
{
  public sealed record GetTokenByPasswordRequest(string Username, string Password);

  public sealed record RegisterRequest(string Username, string? DisplayName, string Password);
  public sealed record RegisterResponse(long UserId, string Token);

  private UserAuthenticationResource.ResourceManager UserAuthentications => ResourceService.GetResourceManager<UserAuthenticationResource.ResourceManager>();
  private UserResource.ResourceManager Users => ResourceService.GetResourceManager<UserResource.ResourceManager>();

  [HttpPost("/auth/register/username-check")]
  public async Task<IActionResult> CheckUsername([FromBody] string username)
  {
    return await ResourceService.Transact((transaction, cancellationToken) =>
    {
      return Ok(ResourceService.GetResourceManager<UserResource.ResourceManager>().ValidateUsername(transaction, username));
    });
  }

  [EnableRateLimiting(Services.WebService.RATE_LIMIT_AUTH)]
  [HttpPut("/auth/register")]
  public async Task<IActionResult> Register([FromBody] RegisterRequest registration)
  {
    UserResource.UserPair? userPair = null;
    string? token = null;

    try
    {
      await ResourceService.Transact((transaction, cancellationToken) =>
      {
        userPair = ResourceService.GetResourceManager<UserResource.ResourceManager>().Create(transaction, registration.Username, registration.DisplayName, registration.Password, cancellationToken);
        token = ResourceService.GetResourceManager<UserAuthenticationResource.ResourceManager>().CreateSessionToken(transaction, userPair.User, userPair.AuthenticationToken);
      });

      return Ok(new RegisterResponse(userPair!.User.Id, token!));
    }
    catch (ArgumentException exception)
    {
      return Error(exception.Message);
    }
  }

  [EnableRateLimiting(Services.WebService.RATE_LIMIT_AUTH)]
  [HttpPost("/auth/get-token-by-password")]
  public async Task<IActionResult> GetTokenByPassword([FromBody] GetTokenByPasswordRequest credentials)
  {
    string? token = null;

    await ResourceService.Transact((transaction, cancellationToken) =>
    {
      if (
        Users.TryGetByUsername(transaction, credentials.Username, out UserResource? user, cancellationToken) &&
        UserAuthentications.TryGetByPayload(transaction, user, Encoding.UTF8.GetBytes(credentials.Password), UserAuthenticationResource.UserAuthenticationType.Password, out UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken)
      )
      {
        token = $"{user.Id}.{UserAuthentications.CreateSessionToken(transaction, user, userAuthenticationToken)}";
      }
    });

    return token != null ? Ok(token) : Error();
  }

  [HttpGet("/auth/verify")]
  public async Task<IActionResult> Get()
  {
    try
    {
      UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = null;

      return await ResourceService.Transact((transaction, cancellationToken) => TryGetUserAuthentication(transaction, out userAuthenticationToken))
        ? Ok()
        : Error("Invalid session token.", 401);
    }
    catch (Exception exception)
    {
      return Error(exception);
    }
  }
}
