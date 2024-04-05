namespace RizzziGit.EnderBytes.Resources;

public abstract partial class Resource<M, D, R>
{
  public abstract partial class ResourceManager
  {
    public sealed record OrderByClause(string Column, OrderByClause.OrderBy Order = OrderByClause.OrderBy.Ascending)
    {
      public enum OrderBy { Ascending, Descending }

      public string Apply() => $"{Column} {Order switch
      {
        OrderBy.Ascending => "asc",
        OrderBy.Descending => "desc",

        _ => throw new ArgumentException(nameof(Order))
      }}";
    }
  }
}
