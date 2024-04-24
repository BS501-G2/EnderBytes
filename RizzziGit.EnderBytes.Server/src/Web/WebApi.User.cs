using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;

public sealed partial class WebApi
{
  [Route("~/user/@{username}")]
  [HttpGet]
  public async Task<ActionResult> GetByUsername(string username)
  {
    return await Run(async () =>
    {
      if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
      {
        return Error(401);
      }

      UserManager.Resource? user = null;
      if ((user = await ResourceService.Transact((transaction, cancellationToken) => GetResourceManager<UserManager>().GetByUsername(transaction, username, cancellationToken))) == null)
      {
        return Error(404);
      }

      return Data(user);
    });
  }

  [Route("~/user/:{userId}")]
  [HttpGet]
  public async Task<ActionResult> GetById(long userId)
  {
    return await Run(async () =>
    {
      if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
      {
        return Error(401);
      }

      UserManager.Resource? user = null;
      if ((user = await ResourceService.Transact((transaction, cancellationToken) => GetResourceManager<UserManager>().GetById(transaction, userId, cancellationToken))) == null)
      {
        return Error(404);
      }

      return Data(user);
    });
  }

  [Route("~/user/!me")]
  [HttpGet]
  public async Task<ActionResult> GetUserSelf()
  {
    return await Run(() =>
    {
      if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
      {
        return Task.FromResult<Result>(Error(401));
      }

      return Task.FromResult<Result>(Data(userAuthenticationToken.User));
    });

  }

  public sealed record UpdateUserRequest(string Username, string LastName, string FirstName, string? MiddleName);

  [Route("~/user/:{userId}")]
  [HttpPost]
  public async Task<ActionResult> UpdateUserById(long userId, [FromBody] UpdateUserRequest request)
  {
    return await Run(async () =>
    {
      if (!TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken))
      {
        return Error(401);
      }
      else if (userAuthenticationToken.UserId != userId)
      {
        return Error(403);
      }

      return await ResourceService.Transact<Result>(async (transaction, cancellationToken) =>
      {
        (string Username, string LastName, string FirstName, string? MiddleName) = request;
        await GetResourceManager<UserManager>().Update(transaction, userAuthenticationToken.User, Username, LastName, FirstName, MiddleName);

        return Data();
      });
    });
  }
}
