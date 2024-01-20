namespace RizzziGit.EnderBytes.Resources;

using Services;
using Utilities;

public sealed partial class User
{
  private const string NAME = "User";
  private const int VERSION = 1;

  public new sealed class ResourceManager(ResourceService service) : Resource<ResourceManager, ResourceData, User>.ResourceManager(service, service.Server.MainDatabase, NAME, VERSION)
  {
    protected override User CreateResourceClass(ResourceRecord record) => new(this, record);

    public (User User, UserAuthentication UserAuthentication) Create(string username, string displayName, UserAuthenticationType userAuthenticationType, byte[] payload, CancellationToken cancellationToken = default) => RunTransaction((cancellationToken) =>
    {
      if (Collection.FindOne((record) => record.Data.Username.Equals(username), cancellationToken: cancellationToken) != null)
      {
        throw new ArgumentException("Username already exists.", nameof(username));
      }

      User user = Insert(new(username, displayName), cancellationToken);
      UserAuthentication userAuthentication = Main.UserAuthentications.Create(user, userAuthenticationType, payload, cancellationToken);

      return (user, userAuthentication);
    }, cancellationToken: cancellationToken);

    public User? GetByUsername(string username, CancellationToken cancellationToken = default) => RunTransaction((cancellationToken) =>
    {
      ResourceRecord? record = Collection.FindOne((record) => record.Data.Username.Equals(username), cancellationToken: cancellationToken);

      return record != null ? ResolveResource(record) : null;
    }, cancellationToken: cancellationToken);
  }
}
