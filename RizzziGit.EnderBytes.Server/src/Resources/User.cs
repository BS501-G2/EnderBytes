namespace RizzziGit.EnderBytes.Resources;

using Framework.Collections;
using Utilities;
using Services.Resource;

public sealed partial class User(User.ResourceManager manager, Resource<User.ResourceManager, User.ResourceData, User>.ResourceRecord record) : Resource<User.ResourceManager, User.ResourceData, User>(manager, record)
{
  private const string NAME = "User";
  private const int VERSION = 1;

  public new sealed class ResourceManager(ResourceService.Main service) : Resource<ResourceManager, ResourceData, User>.ResourceManager(service, service.Server.MainDatabase, NAME, VERSION)
  {
    public new readonly ResourceService.Main Service = service;

    protected override User CreateResourceClass(ResourceRecord record) => new(this, record);

    public (User User, UserAuthentication UserAuthentication) Create(string username, string displayName, UserAuthenticationType userAuthenticationType, byte[] payload, CancellationToken cancellationToken = default) => RunTransaction((cancellationToken) =>
    {
      if (Collection.FindOne((record) => record.Data.Username.Equals(username), cancellationToken: cancellationToken) != null)
      {
        throw new ArgumentException("Username already exists.", nameof(username));
      }

      User user = Insert(new(username, displayName), cancellationToken);
      UserAuthentication userAuthentication = Service.UserAuthentications.Create(user, userAuthenticationType, payload, cancellationToken);

      return (user, userAuthentication);
    }, cancellationToken: cancellationToken);

    public User? GetByUsername(string username, CancellationToken cancellationToken = default) => RunTransaction((cancellationToken) =>
    {
      ResourceRecord? record = Collection.FindOne((record) => record.Data.Username.Equals(username), cancellationToken: cancellationToken);

      return record != null ? ResolveResource(record) : null;
    }, cancellationToken: cancellationToken);
  }

  public new sealed record ResourceData(
    string Username,
    string? DisplayName
  ) : Resource<ResourceManager, ResourceData, User>.ResourceData;
}
