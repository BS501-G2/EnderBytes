namespace RizzziGit.EnderBytes.Services;

using Framework.Services;

using Core;
using Protocols;

public sealed partial class ProtocolService
{
  public abstract class Protocol(ProtocolService service, string name) : Service(name, service)
  {
    public readonly ProtocolService Service = service;
  }
}
