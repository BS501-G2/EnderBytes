using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace RizzziGit.EnderBytes.Services;

public interface IClientRequest;
internal interface IPrivateClientRequest : IClientRequest;

public abstract record ClientRequest : IClientRequest
{
  public static byte[] Serialize(ClientRequest request) => [(byte)request.Code, .. request.ToBson()];

  internal ClientRequest(ClientRequestCode code)
  {
    Code = code;
  }

  public readonly ClientRequestCode Code;
}

public enum ClientRequestCode : byte
{
  Login, Logout, AccountInfo
}

public abstract record ClientRequest<Req, Res>(ClientRequestCode Code) : ClientRequest(Code), IClientRequest where Req : ClientRequest<Req, Res> where Res : ClientResponse<Req, Res>;

public sealed record LoginRequest(string Username, byte[] Payload) : ClientRequest<LoginRequest, LoginResponse>(ClientRequestCode.Login), IPrivateClientRequest;
public sealed record LogoutRequest() : ClientRequest<LogoutRequest, LogoutResponse>(ClientRequestCode.Logout), IPrivateClientRequest;
public sealed record AccountRequest() : ClientRequest<AccountRequest, AccountResponse>(ClientRequestCode.AccountInfo), IPrivateClientRequest;

public interface IClientResponse;
internal interface IPrivateClientResponse : IClientResponse;

public abstract record ClientResponse : IClientResponse
{
  internal ClientResponse(ClientResponseCode code)
  {
    Code = code;
  }

  public readonly ClientResponseCode Code;
}

public enum ClientResponseCode : byte
{
  OK, InvalidCredentials, LoginRequired, NotLoggedIn
}

public abstract record ClientResponse<Req, Res>(ClientResponseCode Code) : ClientResponse(Code), IClientResponse where Req : ClientRequest<Req, Res> where Res : ClientResponse<Req, Res>;

public sealed record LoginResponse(ClientResponseCode Code) : ClientResponse<LoginRequest, LoginResponse>(Code), IPrivateClientResponse;
public sealed record LogoutResponse(ClientResponseCode Code) : ClientResponse<LogoutRequest, LogoutResponse>(Code), IPrivateClientResponse;
public sealed record AccountResponse(ClientResponseCode Code, string? Username) : ClientResponse<AccountRequest, AccountResponse>(Code), IPrivateClientResponse;
