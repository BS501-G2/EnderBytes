namespace RizzziGit.EnderBytes.Services;

using System.Data.SQLite;
using Core;
using DatabaseWrappers;
using Resources;

public sealed partial class ResourceService : Server.SubService
{
    public ResourceService(Server server)
        : base(server, "Resources")
    {
        ResourceManagers = [];

        ResourceManagers.Add(new UserManager(this));
        ResourceManagers.Add(new UserAuthenticationManager(this));
        ResourceManagers.Add(new UserAuthenticationSessionTokenManager(this));
        ResourceManagers.Add(new UserConfigurationManager(this));
        ResourceManagers.Add(new FileManager(this));
        ResourceManagers.Add(new FileAccessManager(this));
        ResourceManagers.Add(new FileContentManager(this));
        ResourceManagers.Add(new FileContentVersionManager(this));
        ResourceManagers.Add(new FileDataManager(this));
        ResourceManagers.Add(new FileBlobManager(this));
    }

    private Database? Database;
    private readonly List<ResourceManager> ResourceManagers;

    public T GetManager<T>()
        where T : ResourceManager
    {
        foreach (ResourceManager resourceManager in ResourceManagers)
        {
            if (resourceManager is T t)
            {
                return t;
            }
        }

        throw new ArgumentException("Specified type is not available.");
    }

    protected override async Task OnStart(CancellationToken cancellationToken)
    {
        Database = new SQLiteDatabase(
            new SQLiteConnectionStringBuilder
            {
                DataSource = Path.Join(Server.Configuration.WorkingPath, "Database.db")
            }
        );

        await Task.WhenAll(
            ResourceManagers.Select((resourceManager) => resourceManager.Start(cancellationToken))
        );
    }

    protected override async Task OnRun(CancellationToken cancellationToken)
    {
        await await Task.WhenAny(WatchDog([.. ResourceManagers], cancellationToken));
    }

    protected override async Task OnStop(Exception? exception = null)
    {
        foreach (ResourceManager resourceManager in ResourceManagers.Reverse<ResourceManager>())
        {
            await resourceManager.Stop();
        }

        Database?.Dispose();
        Database = null;
    }
}
