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
    if ((context.Request.Path == "/admin/setup") == (await server.ResourceService.Transact((transaction, _) => transaction.GetManager<UserManager>().CountUsers(transaction)) == 0))
    {
      await next();
    }
    else
    {
      context.Response.StatusCode = 409;
      return;
    }
  }

  [Route("/admin/setup")]
  public async Task<ActionResult> Create()
  {
    return Ok();
  }
}
