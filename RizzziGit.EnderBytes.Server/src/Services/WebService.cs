using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace RizzziGit.EnderBytes.Services;

using Core;
using Web;
using Utilities;
using Microsoft.Extensions.FileProviders;

public sealed class WebService(Server server) : Server.SubService(server, "WebService")
{
  private WebApplication CreateWebApplication()
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder();

    // builder.Logging.SetMinimumLevel(LogLevel.None);
    builder.WebHost.ConfigureKestrel((kestrelConfiguration) =>
    {
      kestrelConfiguration.AllowAlternateSchemes = true;
      kestrelConfiguration.AddServerHeader = false;
      kestrelConfiguration.AllowHostHeaderOverride = true;
      kestrelConfiguration.Listen(IPAddress.Any, Server.Configuration.HttpClientPort, (options) =>
      {
        options.Protocols = HttpProtocols.Http1;
      });

      if (Server.Configuration.HttpsClient != null)
      {
        kestrelConfiguration.Listen(IPAddress.Any, Server.Configuration.HttpsClient.Port, (options) =>
        {
          options.Protocols = HttpProtocols.Http1AndHttp2AndHttp3;
          options.UseHttps(Server.Configuration.HttpsClient.CertificatePath, Server.Configuration.HttpsClient.CertificatePassword);
        });
      }
    });

    builder.Services.AddRazorComponents().AddInteractiveServerComponents();

    WebApplication app = builder.Build();

    if (Server.Configuration.HttpsClient != null)
    {
      app.UseHttpsRedirection();
    }

    app.Use(async (HttpContext context, RequestDelegate next) =>
    {
      if (context.Request.Path == "/ws" && context.WebSockets.IsWebSocketRequest)
      {
        await Server.ClientService.HandleWebSocket(await context.WebSockets.AcceptWebSocketAsync());

        context.Connection.RequestClose();
        return;
      }

      await next(context);
    });

    app.Environment.ContentRootPath = Path.Join(Environment.CurrentDirectory, "src", "Web");
    app.Environment.WebRootPath = Path.Join("Static");

    app.Environment.ContentRootPath = Path.Join(Environment.CurrentDirectory, "src", "Web");

    app.UseStaticFiles(new StaticFileOptions
    {
      FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.ContentRootPath, "Static"))
    });
    app.UseAntiforgery();

    app.MapRazorComponents<App>()
      .AddInteractiveServerRenderMode();

    return app;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await using WebApplication webApplication = CreateWebApplication();

    try
    {
      await webApplication.RunAsync(cancellationToken);
    }
    catch (Exception exception)
    {
      if (!exception.IsDueToCancellationToken(cancellationToken))
      {
        throw;
      }

      await webApplication.WaitForShutdownAsync(CancellationToken.None);
    }
  }
}
