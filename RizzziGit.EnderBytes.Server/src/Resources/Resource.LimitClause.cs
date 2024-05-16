namespace RizzziGit.EnderBytes.Resources;

public abstract partial class ResourceManager<M, R>
{
    public sealed record LimitClause(long Limit, long? Skip = null)
    {
        public string Apply() => Skip != null ? $"{Limit} offset {Skip}" : $"{Limit}";
    }
}
