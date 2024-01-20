namespace RizzziGit.EnderBytes.Resources;

public sealed partial class User
{
  public new sealed record ResourceData(
    string Username,
    string? DisplayName
  ) : Resource<ResourceManager, ResourceData, User>.ResourceData;

  public string Username => Data.Username;
  public string? DisplayName => Data.DisplayName;
}
