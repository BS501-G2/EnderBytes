using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using  Services;

public sealed partial class WebApi
{
  public sealed record PasswordLoginRequest(string Username, string Password);
  public sealed record PasswordLoginResponse(long UserId, string Token);

  [Route("/auth/password-login")]
  [HttpPost]
  public Task<ActionResult<PasswordLoginResponse>> Login([FromBody] PasswordLoginRequest request) => ResourceService.Transact<ActionResult<PasswordLoginResponse>>((transaction, cancellationToken) =>
  {
    if (TryGetUserAuthenticationToken(out _))
    {
      return Conflict();
    }

    (string Username, string Password) = request;

    if (
      !GetResourceManager<UserManager>().TryGetByUsername(transaction, Username, out UserManager.Resource? user, cancellationToken) ||
      !GetResourceManager<UserAuthenticationManager>().TryGetByPayload(transaction, user, Encoding.UTF8.GetBytes(Password), UserAuthenticationType.Password, out UserAuthenticationToken? userAuthenticationToken)
    )
    {
      return Unauthorized();
    }

    string token = GetResourceManager<UserAuthenticationManager>().CreateSessionToken(transaction, user, userAuthenticationToken, cancellationToken);
    Server.ResourceService.GetManager<UserAuthenticationManager>().TryTruncateSessionTokens(transaction, user, cancellationToken);

    return Ok(new PasswordLoginResponse(user.Id, token));
  });

  [Route("/auth/logout")]
  public async Task<ActionResult> Logout()
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized(401);
    }

    await ResourceService.Transact((transaction, cancellationToken) =>
    {
      GetResourceManager<UserAuthenticationManager>().Delete(transaction, userAuthenticationToken.UserAuthentication, cancellationToken);
    });

    return Ok();
  }

}
