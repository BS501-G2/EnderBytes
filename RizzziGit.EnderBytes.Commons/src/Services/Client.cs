using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace RizzziGit.EnderBytes.Services;

using Commons.Net;

public interface IClientRequest;
internal interface IPrivateClientRequest : IClientRequest;

public abstract record ClientRequest(ClientRequestCode Code) : IClientRequest
{
  public HybridWebSocket.Payload Serialize() => new((uint)Code, this.ToBson());

  public static ClientRequest Deserialize(HybridWebSocket.Payload payload) => (ClientRequestCode)payload.Code switch
  {
    ClientRequestCode.Login => Deserialize<LoginRequest>(payload.Buffer.ToByteArray()),
    ClientRequestCode.Logout => Deserialize<LogoutRequest>(payload.Buffer.ToByteArray()),
    ClientRequestCode.AccountInfo => Deserialize<AccountInfoRequest>(payload.Buffer.ToByteArray()),

    _ => throw new NotImplementedException()
  };

  protected static T Deserialize<T>(byte[] Payload) where T : ClientRequest => BsonSerializer.Deserialize<T>(Payload[1..]);
}

public enum ClientRequestCode : uint
{
  Login, Logout, AccountInfo
}

public abstract record ClientRequest<Req, Res>(ClientRequestCode Code) : ClientRequest(Code), IClientRequest where Req : ClientRequest where Res : ClientResponse;

public sealed record LoginRequest(string Username, byte[] Payload) : ClientRequest<LoginRequest, LoginResponse>(ClientRequestCode.Login), IPrivateClientRequest;
public sealed record LogoutRequest() : ClientRequest<LogoutRequest, LogoutResponse>(ClientRequestCode.Logout), IPrivateClientRequest;
public sealed record AccountInfoRequest() : ClientRequest<AccountInfoRequest, AccountInfoResponse>(ClientRequestCode.AccountInfo), IPrivateClientRequest;

public interface IClientResponse;
internal interface IPrivateClientResponse : IClientResponse;

public abstract record ClientResponse(ClientResponseCode Code) : IClientResponse
{
  public HybridWebSocket.Payload Serialize() => new((uint)Code, this.ToBson());

  public static ClientResponse Deserialize(HybridWebSocket.Payload payload) => (ClientResponseCode)payload.Code switch
  {
    ClientResponseCode.Login => Deserialize<LoginResponse>(payload.Buffer.ToByteArray()),
    ClientResponseCode.Logout => Deserialize<LogoutResponse>(payload.Buffer.ToByteArray()),
    ClientResponseCode.AccountInfo => Deserialize<AccountInfoResponse>(payload.Buffer.ToByteArray()),

    _ => throw new NotImplementedException()
  };

  protected static T Deserialize<T>(byte[] Payload) where T : ClientResponse => BsonSerializer.Deserialize<T>(Payload[1..]);
}

public enum ClientResponseCode : uint
{
  Login,
  Logout,
  AccountInfo,

  Error
}

public abstract record ClientResponse<Req, Res>(ClientResponseCode Code) : ClientResponse(Code), IClientResponse where Req : ClientRequest where Res : ClientResponse;

public sealed record LoginResponse() : ClientResponse<LoginRequest, LoginResponse>(ClientResponseCode.Login), IPrivateClientResponse;
public sealed record LogoutResponse() : ClientResponse<LogoutRequest, LogoutResponse>(ClientResponseCode.Logout), IPrivateClientResponse;
public sealed record AccountInfoResponse(string? Username) : ClientResponse<AccountInfoRequest, AccountInfoResponse>(ClientResponseCode.AccountInfo), IPrivateClientResponse;
public sealed record ErrorResponse(ErrorResponse.ClientErrorResponseCode ErrorCode) : ClientResponse<ClientRequest, ErrorResponse>(ClientResponseCode.Error), IPrivateClientResponse
{
  public enum ClientErrorResponseCode
  {
    InvalidCredentials, AlreadyLoggedIn, LoginRequired
  }

  public void Throw() => throw new InvalidOperationException($"Invalid Operation: {ErrorCode}");
}
