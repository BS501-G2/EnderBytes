using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

public sealed partial class WebApi
{
  [Route("/error")]
  public async Task<ActionResult> Error([FromServices] IHostEnvironment hostEnvironment)
  {
    return await Run(async () =>
    {
      IExceptionHandlerFeature exceptionHandlerFeature = HttpContext.Features.GetRequiredFeature<IExceptionHandlerFeature>();

      return Error(500, exceptionHandlerFeature.Error);
    });
  }
}
