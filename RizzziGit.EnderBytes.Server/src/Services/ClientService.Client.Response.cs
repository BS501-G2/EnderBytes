using RizzziGit.EnderBytes.Resources;

namespace RizzziGit.EnderBytes.Services;

public sealed partial class ClientService
{
  private interface IPrivateResponse : Client.IResponse;

  public abstract partial class Client
  {
    public interface IResponse;

    public abstract record Response<Req, Res> : IResponse where Req : Request<Req, Res> where Res : Response<Req, Res>;

    public sealed record LoginResponse(LoginResponse.LoginResult Result) : Response<LoginRequest, LoginResponse>(), IPrivateResponse
    {
      public enum LoginResult { OK, InvalidUserAuthenticationToken }
    }

    public sealed record LogoutResponse(LogoutResponse.LogoutResult Result) : Response<LogoutRequest, LogoutResponse>(), IPrivateResponse
    {
      public enum LogoutResult { OK, NotLoggedIn }
    }

    public sealed record AccountResponse(UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken = null) : Response<AccountRequest, AccountResponse>(), IPrivateResponse;
  }
}
