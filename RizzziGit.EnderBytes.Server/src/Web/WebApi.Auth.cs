using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using Services;

public sealed partial class WebApi
{
  public sealed record PasswordLoginRequest(string Username, string Password);
  public sealed record PasswordLoginResponse(long UserId, string Token);

  [Route("/auth/password-login")]
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

  [Route("/auth/logout")]
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
