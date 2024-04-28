namespace RizzziGit.EnderBytes.Resources;

public abstract partial class ResourceManager<M, R>
{
  public sealed record LimitClause(int Limit, int? Skip = null)
  {
    public string Apply() => $"{(Skip != null ? $"{Skip}, " : "")}{Limit}";
  }
}
