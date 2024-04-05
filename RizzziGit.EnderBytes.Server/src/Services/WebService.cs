using System.Net;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace RizzziGit.EnderBytes.Services;

using Core;
using Utilities;

public sealed partial class WebService(Server server) : Server.SubService(server, "WebService")
{
  private WebApplication CreateWebApplication(CancellationToken cancellationToken = default)
  {
    WebApplicationBuilder builder = WebApplication.CreateBuilder();

    string corsPolicy = "EnderBytesCorsPolicy";

    builder.Services
      .AddCors((setup) =>
      {
        setup.AddPolicy(corsPolicy, (policy) =>
        {
          policy.WithOrigins("http://localhost:8081", "http://10.1.0.1:8081", "http://10.1.0.117:8081");
          policy.WithHeaders("*");
          policy.WithMethods("*");
        });
      })
      .AddSingleton(Server)
      .AddControllers();

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

    WebApplication app = builder.Build();

    app.UseCors(corsPolicy);
    app.UseWebSockets();
    if (Server.Configuration.HttpsClient != null)
    {
      app.UseHttpsRedirection();
    }

    app.Run(async (context) =>
    {
      if (!context.WebSockets.IsWebSocketRequest)
      {
        context.Response.StatusCode = 400;
        return;
      }

      await Server.ClientService.HandleUserClient(await context.WebSockets.AcceptWebSocketAsync(), cancellationToken);
    });

    return app;
  }

  protected override async Task OnRun(CancellationToken cancellationToken)
  {
    await using WebApplication webApplication = CreateWebApplication(cancellationToken);

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
