using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Services;
using Resources;

public sealed partial class WebApi
{
  [Route("/user/@{username}")]
  [HttpGet]
  public async Task<ActionResult<UserManager.Resource>> GetUser(string username)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken)) {
      return StatusCode(401);
    }

    UserManager.Resource? user = null;
    if (!await ResourceService.Transact((transaction, cancellationToken) => GetResourceManager<UserManager>().TryGetByUsername(transaction, username, out user, cancellationToken)))
    {
      return StatusCode(404);
    }

    return Ok(user);
  }

  [Route("/user/:{userId}")]
  [HttpGet]
  public async Task<ActionResult<UserManager.Resource>> GetUserById(long userId)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return StatusCode(401);
    }

    UserManager.Resource? user = null;
    if (!await ResourceService.Transact((transaction, cancellationToken) => GetResourceManager<UserManager>().TryGetById(transaction, userId, out user, cancellationToken)))
    {
      return StatusCode(404);
    }

    return Ok(user);
  }

  public sealed record UpdateUserRequest(string Username, string LastName, string FirstName, string? MiddleName);

  [Route("/user/:{userId}")]
  [HttpPost]
  public async Task<ActionResult<bool>> UpdateUserById(long userId, [FromBody] UpdateUserRequest request)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return StatusCode(401);
    } else if (userAuthenticationToken.UserId != userId)
    {
      return StatusCode(403);
    }

    return Ok(await ResourceService.Transact((transaction, cancellationToken) =>
    {
      (string Username, string LastName, string FirstName, string? MiddleName) = request;

      return GetResourceManager<UserManager>().Update(transaction, userAuthenticationToken.User, Username, LastName, FirstName, MiddleName);
    }));
  }
}
