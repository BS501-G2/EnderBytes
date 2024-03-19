using RizzziGit.EnderBytes.Resources;

namespace RizzziGit.EnderBytes.Services;

public sealed partial class ClientService
{
  public abstract partial class Client(UserAuthenticationResource.UserAuthenticationToken? userAuthenticationToken = null)
  {
    public UserAuthenticationResource.UserAuthenticationToken? UserAuthenticationToken { get; private set; } = userAuthenticationToken;

    public virtual Task<Res> ProcessRequest<Req, Res>(Req rawRequest, CancellationToken cancellationToken = default) where Req : Request<Req, Res> where Res : Response<Req, Res> => Task.FromResult((Res)(object)(rawRequest switch
    {
      LoginRequest request => ProcessRequest(request, cancellationToken),
      LogoutRequest request => ProcessRequest(request, cancellationToken),
      AccountRequest request => ProcessRequest(request, cancellationToken),

      _ => throw new ArgumentException("Invalid command.", nameof(rawRequest)),
    }));

    public LoginResponse ProcessRequest(LoginRequest loginRequest, CancellationToken cancellationToken = default)
    {
      lock (this)
      {
        UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken = loginRequest.UserAuthenticationToken;

        userAuthenticationToken.TryEnter(() =>
        {
          UserAuthenticationToken = userAuthenticationToken;

          return new LoginResponse(LoginResponse.LoginResult.OK);
        }, out LoginResponse? result);

        return result ?? new LoginResponse(LoginResponse.LoginResult.InvalidUserAuthenticationToken);
      }
    }

    public LogoutResponse ProcessRequest(LogoutRequest logoutRequest, CancellationToken cancellationToken = default)
    {
      lock (this)
      {
        bool isLoggedIn = UserAuthenticationToken?.IsValid == true;

        UserAuthenticationToken = null;

        return new(isLoggedIn ? LogoutResponse.LogoutResult.OK : LogoutResponse.LogoutResult.NotLoggedIn);
      }
    }

    public AccountResponse ProcessRequest(AccountRequest accountRequest, CancellationToken cancellationToken = default)
    {
      lock (this)
      {
        return new(UserAuthenticationToken?.TryEnter(() => UserAuthenticationToken, out UserAuthenticationResource.UserAuthenticationToken userAuthenticationToken) ?? false ? UserAuthenticationToken : null);
      }
    }
  }
}
