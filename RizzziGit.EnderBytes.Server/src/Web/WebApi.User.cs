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
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    UserManager.Resource? user = null;
    if ((user = await ResourceService.Transact((transaction, cancellationToken) => GetResourceManager<UserManager>().GetByUsername(transaction, username, cancellationToken))) == null)
    {
      return NotFound();
    }

    return Ok(user);
  }

  [Route("/user/:{userId}")]
  [HttpGet]
  public async Task<ActionResult<UserManager.Resource>> GetUserById(long userId)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    UserManager.Resource? user = null;
    if ((user = await ResourceService.Transact((transaction, cancellationToken) => GetResourceManager<UserManager>().GetById(transaction, userId, cancellationToken))) == null)
    {
      return NotFound();
    }

    return Ok(user);
  }

  [Route("/user/!me")]
  [HttpGet]
  public ActionResult<UserManager.Resource> GetUserSelf()
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }

    return Ok(userAuthenticationToken.User);
  }

  public sealed record UpdateUserRequest(string Username, string LastName, string FirstName, string? MiddleName);

  [Route("/user/:{userId}")]
  [HttpPost]
  public async Task<ActionResult<bool>> UpdateUserById(long userId, [FromBody] UpdateUserRequest request)
  {
    if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
    {
      return Unauthorized();
    }
    else if (userAuthenticationToken.UserId != userId)
    {
      return Forbid();
    }

    return Ok(await ResourceService.Transact((transaction, cancellationToken) =>
    {
      (string Username, string LastName, string FirstName, string? MiddleName) = request;

      return GetResourceManager<UserManager>().Update(transaction, userAuthenticationToken.User, Username, LastName, FirstName, MiddleName);
    }));
  }
}
