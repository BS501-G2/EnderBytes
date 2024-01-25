namespace RizzziGit.EnderBytes.Resources;

public abstract partial class Resource<M, D, R>
{
  public abstract partial class ResourceManager
  {
    public sealed record LimitClause(int Limit, int? Skip = null)
    {
      public string Apply() => $"{(Skip != null ? $"{Skip}, " : "")}{Limit}";
    }
  }
}
