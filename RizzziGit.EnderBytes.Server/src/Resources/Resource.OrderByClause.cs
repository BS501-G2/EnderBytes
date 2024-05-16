namespace RizzziGit.EnderBytes.Resources;

public abstract partial class ResourceManager<M, R>
{
    public sealed record OrderByClause(string Column, bool Descending = false)
    {
        public string Apply() => $"{Column} {(Descending ? "desc" : "asc")}";
    }
}
