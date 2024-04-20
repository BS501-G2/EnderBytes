using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Core;
using Resources;

public sealed partial class WebApi
{
  public sealed record AdminCreateRequest()
  {
  }

  [NonAction]
  public static async Task CheckAdmin(Server server, HttpContext context, Func<Task> next)
  {
    if (context.Request.Path == "/admin/setup" != (await server.ResourceService.Transact((transaction, _) => transaction.GetManager<UserManager>().CountUsers(transaction)) == 0))
    {
      new ConflictResult().ExecuteResult(new()
      {
        HttpContext = context
      });

      return;
    }

    await next();
  }

  [Route("/admin/setup")]
  public Task<ActionResult> Create()
  {
    return Task.FromResult<ActionResult>(Ok());
  }
}
