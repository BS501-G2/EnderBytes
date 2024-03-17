namespace RizzziGit.EnderBytes.Services;

using Resources;

public sealed partial class ClientService
{
  private interface IPrivateRequest : Client.IRequest;

  public abstract partial class Client
  {
    public interface IRequest;

    public abstract record Request<Req, Res> : IRequest where Req : Request<Req, Res> where Res : Response<Req, Res>;

    public sealed record LoginRequest(UserAuthenticationResource.UserAuthenticationToken UserAuthenticationToken) : Request<LoginRequest, LoginResponse>(), IPrivateRequest;
    public sealed record LogoutRequest() : Request<LogoutRequest, LogoutResponse>(), IPrivateRequest;
    public sealed record AccountRequest() : Request<AccountRequest, AccountResponse>(), IPrivateRequest;
  }
}
