using System.Data.Common;
using Newtonsoft.Json;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Memory;
using Services;
using Utilities;
using ResourceManager = ResourceManager<FileManager, FileManager.Resource>;

public sealed record FileCreationResult(FileManager.Resource File, KeyService.AesPair FileKey);

public sealed partial class FileManager : ResourceManager
{
    public const string NAME = "File";
    public const int VERSION = 1;

    public const string COLUMN_TRASH_TIME = "TrashTime";
    public const string COLUMN_DOMAIN_USER_ID = "DomainUserId";
    public const string COLUMN_AUTHOR_USER_ID = "AuthorUserId";
    public const string COLUMN_PARENT_ID = "ParentId";
    public const string COLUMN_NAME = "Name";
    public const string COLUMN_IS_FOLDER = "IsFolder";
    public const string COLUMN_AES_KEY = "AesKey";

    private const string DELETE_CONTEXT_USER_AUTHENTICATION_TOKEN =
        $"{NAME}_Delete_UserAuthenticationToken";

    public new sealed record Resource(
        long Id,
        long CreateTime,
        long UpdateTime,
        long? TrashTime,
        long DomainUserId,
        long AuthorUserId,
        long? ParentId,
        string Name,
        bool IsFolder,
        byte[] AesKey
    ) : ResourceManager.Resource(Id, CreateTime, UpdateTime)
    {
        [JsonIgnore]
        public readonly byte[] AesKey = AesKey;
    }

    public sealed class NotAFolderException(Resource file)
        : Exception($"#{file.Id} is not a folder.")
    {
        public readonly Resource File = file;
    }

    public sealed class NotAFileException(Resource file) : Exception($"#{file.Id} is not a file.")
    {
        public readonly Resource File = file;
    }

    public sealed class InvalidAccessException(Resource file, UserManager.Resource? user)
        : Exception($"#{file.Id} is not accessible by the current user.")
    {
        public readonly Resource File = file;
        public readonly UserManager.Resource? User = user;
    }

    public sealed class InvalidTrashOperationException(Resource file)
        : Exception($"#{file.Id} is a root folder and cannot be moved to trash.");

    public sealed class InvalidMoveException(Resource file, Resource newParent)
        : Exception(
            $"#{file.Id} cannot be moved to #{newParent.Id} because the file is already in that folder."
        );

    public sealed class InvalidRootDeleteException(Resource file)
        : Exception($"#{file.Id} cannot be deleted because it is a root folder.");

    public FileManager(ResourceService service)
        : base(service, NAME, VERSION)
    {
        GetManager<UserManager>()
            .RegisterDeleteHandler(
                (transaction, user) =>
                    Delete(
                        transaction,
                        new WhereClause.CompareColumn(COLUMN_AUTHOR_USER_ID, "=", user.Id)
                    )
            );

        RegisterDeleteHandler(
            async (transaction, resource) =>
                await Delete(
                    transaction,
                    new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", resource.Id)
                )
        );
    }

    protected override Resource ToResource(
        DbDataReader reader,
        long id,
        long createTime,
        long updateTime
    ) =>
        new(
            id,
            createTime,
            updateTime,
            reader.GetInt64Optional(reader.GetOrdinal(COLUMN_TRASH_TIME)),
            reader.GetInt64(reader.GetOrdinal(COLUMN_DOMAIN_USER_ID)),
            reader.GetInt64(reader.GetOrdinal(COLUMN_AUTHOR_USER_ID)),
            reader.GetInt64Optional(reader.GetOrdinal(COLUMN_PARENT_ID)),
            reader.GetString(reader.GetOrdinal(COLUMN_NAME)),
            reader.GetBoolean(reader.GetOrdinal(COLUMN_IS_FOLDER)),
            reader.GetBytes(reader.GetOrdinal(COLUMN_AES_KEY))
        );

    protected override async Task Upgrade(
        ResourceService.Transaction transaction,
        int oldVersion = 0
    )
    {
        if (oldVersion < 1)
        {
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_TRASH_TIME} integer null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_DOMAIN_USER_ID} integer not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_AUTHOR_USER_ID} integer not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_PARENT_ID} integer null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_NAME} varchar(128) not null collate nocase;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_IS_FOLDER} integer(1) not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_AES_KEY} blob not null;"
            );
        }
    }

    public async Task<Resource> GetRootFromUser(
        ResourceService.Transaction transaction,
        UserAuthenticationToken userAuthenticationToken
    )
    {
        Resource? resource;

        if (
            (
                resource = await SelectFirst(
                    transaction,
                    new WhereClause.CompareColumn(
                        COLUMN_AUTHOR_USER_ID,
                        "=",
                        userAuthenticationToken.UserId
                    )
                )
            ) == null
        )
        {
            KeyService.AesPair newKey = Service.Server.KeyService.GetNewAesPair();

            resource = await InsertAndGet(
                transaction,
                new(
                    (COLUMN_DOMAIN_USER_ID, userAuthenticationToken.UserId),
                    (COLUMN_AUTHOR_USER_ID, userAuthenticationToken.UserId),
                    (COLUMN_PARENT_ID, null),
                    (COLUMN_NAME, "/"),
                    (COLUMN_IS_FOLDER, true),
                    (COLUMN_AES_KEY, userAuthenticationToken.Encrypt(newKey.Serialize()))
                )
            );
        }

        return resource;
    }

    public async Task<Resource[]> PathChain(ResourceService.Transaction transaction, Resource file)
    {
        async Task<Resource[]> getChain()
        {
            Resource? resource = file;
            List<Resource> chain = [];

            while (resource.ParentId != null)
            {
                chain.Add(resource);

                if ((resource = await GetById(transaction, (long)resource.ParentId)) == null)
                {
                    break;
                }
            }

            return chain.ToArray();
        }

        return (await getChain()).Reverse().ToArray();
    }

    public async Task<FileCreationResult> Create(
        ResourceService.Transaction transaction,
        Resource parentFolder,
        string name,
        bool isFolder,
        UserAuthenticationToken userAuthenticationToken
    )
    {
        if (!parentFolder.IsFolder)
        {
            throw new NotAFolderException(parentFolder);
        }

        KeyService.AesPair newKey = Service.Server.KeyService.GetNewAesPair();
        Resource file = await InsertAndGet(
            transaction,
            new(
                (COLUMN_DOMAIN_USER_ID, parentFolder.DomainUserId),
                (COLUMN_AUTHOR_USER_ID, userAuthenticationToken.UserId),
                (COLUMN_PARENT_ID, parentFolder.Id),
                (COLUMN_NAME, await ThrowIfInvalidName(transaction, parentFolder, name)),
                (COLUMN_IS_FOLDER, isFolder),
                (
                    COLUMN_AES_KEY,
                    (
                        await GetKeyRequired(
                            transaction,
                            parentFolder,
                            FileAccessExtent.ReadWrite,
                            userAuthenticationToken
                        )
                    ).Encrypt(newKey.Serialize())
                )
            )
        );

        return new(file, newKey);
    }

    public async Task<bool> Move(
        ResourceService.Transaction transaction,
        Resource file,
        Resource newParent,
        string? newName,
        UserAuthenticationToken userAuthenticationToken
    )
    {
        if (!newParent.IsFolder)
        {
            throw new NotAFolderException(newParent);
        }

        Resource[] pathChain = await PathChain(transaction, newParent);
        Resource oldParent = pathChain[^1];
        KeyService.AesPair oldParentKey = await GetKeyRequired(
            transaction,
            file,
            FileAccessExtent.ReadWrite,
            userAuthenticationToken
        );
        KeyService.AesPair newParentKey = await GetKeyRequired(
            transaction,
            newParent,
            FileAccessExtent.ReadWrite,
            userAuthenticationToken
        );

        if (file.Id == newParent.Id || pathChain.Any((fileTest) => fileTest.Id == file.Id))
        {
            throw new InvalidMoveException(file, newParent);
        }

        if (
            await Count(
                transaction,
                new WhereClause.Nested(
                    "and",
                    new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", file.ParentId),
                    new WhereClause.Raw($"{COLUMN_TRASH_TIME} is null"),
                    new WhereClause.CompareColumn(COLUMN_NAME, "=", newName ?? file.Name)
                )
            ) > 0
        )
        {
            int currentNameIter = 1;
            string currentName() => $"{file.Name} ({currentNameIter})";

            while (
                await Count(
                    transaction,
                    new WhereClause.Nested(
                        "and",
                        new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", file.ParentId),
                        new WhereClause.Raw($"{COLUMN_TRASH_TIME} is null"),
                        new WhereClause.CompareColumn(COLUMN_NAME, "=", currentName())
                    )
                ) > 0
            )
            {
                currentNameIter++;
            }

            return await Update(
                transaction,
                file,
                new(
                    (COLUMN_TRASH_TIME, null),
                    (COLUMN_NAME, newName ?? currentName()),
                    (COLUMN_AES_KEY, newParentKey.Encrypt(oldParentKey.Decrypt(file.AesKey)))
                )
            );
        }

        return await Update(
            transaction,
            file,
            new(
                (COLUMN_PARENT_ID, newParent.Id),
                (COLUMN_NAME, newName ?? file.Name),
                (COLUMN_AES_KEY, newParentKey.Encrypt(oldParentKey.Decrypt(file.AesKey)))
            )
        );
    }

    public async Task<bool> Restore(ResourceService.Transaction transaction, Resource file)
    {
        if (file.TrashTime == null)
        {
            return false;
        }

        if (
            await Count(
                transaction,
                new WhereClause.Nested(
                    "and",
                    new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", file.ParentId),
                    new WhereClause.Raw($"{COLUMN_TRASH_TIME} is null"),
                    new WhereClause.CompareColumn(COLUMN_NAME, "=", file.Name)
                )
            ) > 0
        )
        {
            int currentNameIter = 1;
            string currentName() => $"{file.Name} ({currentNameIter})";

            while (
                await Count(
                    transaction,
                    new WhereClause.Nested(
                        "and",
                        new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", file.ParentId),
                        new WhereClause.Raw($"{COLUMN_TRASH_TIME} is null"),
                        new WhereClause.CompareColumn(COLUMN_NAME, "=", currentName())
                    )
                ) > 0
            )
            {
                currentNameIter++;
            }

            return await Update(
                transaction,
                file,
                new((COLUMN_TRASH_TIME, null), (COLUMN_NAME, currentName()))
            );
        }

        return await Update(transaction, file, new((COLUMN_TRASH_TIME, null)));
    }

    public async Task<bool> Trash(ResourceService.Transaction transaction, Resource file)
    {
        if (file.TrashTime != null)
        {
            return false;
        }
        else if (file.ParentId == null)
        {
            throw new InvalidTrashOperationException(file);
        }

        return await Update(
            transaction,
            file,
            new((COLUMN_TRASH_TIME, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
        );
    }

    public async Task<bool> TestAccess(
        ResourceService.Transaction transaction,
        Resource file,
        FileAccessExtent fileAccessExtent,
        UserAuthenticationToken userAuthenticationToken
    )
    {
        return await GetKey(transaction, file, fileAccessExtent, userAuthenticationToken) != null;
    }

    public async Task<KeyService.AesPair> GetKeyRequired(
        ResourceService.Transaction transaction,
        Resource file,
        FileAccessExtent fileAccessExtent,
        UserAuthenticationToken userAuthenticationToken
    )
    {
        return await GetKey(transaction, file, fileAccessExtent, userAuthenticationToken)
            ?? throw new InvalidAccessException(file, userAuthenticationToken.User);
    }

    public async Task<long> CountFiles(
        ResourceService.Transaction transaction,
        Resource folder,
        UserAuthenticationToken userAuthenticationToken
    )
    {
        if (!folder.IsFolder)
        {
            throw new NotAFolderException(folder);
        }

        await GetKey(transaction, folder, FileAccessExtent.ReadOnly, userAuthenticationToken);
        return await Count(
            transaction,
            new WhereClause.Nested(
                "and",
                new WhereClause.CompareColumn(COLUMN_DOMAIN_USER_ID, "=", folder.DomainUserId),
                new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", folder.Id),
                new WhereClause.Raw($"{COLUMN_TRASH_TIME} is null")
            )
        );
    }

    public async Task<Resource[]> ScanFolder(
        ResourceService.Transaction transaction,
        Resource folder,
        UserAuthenticationToken userAuthenticationToken,
        LimitClause? limitClause = null,
        OrderByClause?[]? orderByClause = null,
        bool? isFolder = null
    )
    {
        if (!folder.IsFolder)
        {
            throw new NotAFolderException(folder);
        }

        await GetKey(transaction, folder, FileAccessExtent.ReadOnly, userAuthenticationToken);

        return await Select(
            transaction,
            new WhereClause.Nested(
                "and",
                new WhereClause.CompareColumn(COLUMN_DOMAIN_USER_ID, "=", folder.DomainUserId),
                new WhereClause.CompareColumn(COLUMN_PARENT_ID, "=", folder.Id),
                isFolder != null
                    ? new WhereClause.CompareColumn(COLUMN_IS_FOLDER, "=", isFolder)
                    : null,
                new WhereClause.Raw($"{COLUMN_TRASH_TIME} is null")
            ),
            limitClause,
            orderByClause
        );
    }

    public async Task<KeyService.AesPair?> GetKey(
        ResourceService.Transaction transaction,
        Resource file,
        FileAccessExtent fileAccessExtent,
        UserAuthenticationToken userAuthenticationToken
    )
    {
        if (file.DomainUserId == userAuthenticationToken.UserId)
        {
            async Task<KeyService.AesPair> getKey(Resource file)
            {
                if (file.ParentId != null)
                {
                    return await getKey((await GetById(transaction, (long)file.ParentId))!);
                }

                return KeyService.AesPair.Deserialize(userAuthenticationToken.Decrypt(file.AesKey));
            }

            return await getKey(file);
        }
        else
        {
            FileAccessPoint? accessPoint = await GetManager<FileAccessManager>()
                .GetAccessPoint(transaction, userAuthenticationToken.User, file, fileAccessExtent);

            if (accessPoint != null)
            {
                async Task<KeyService.AesPair?> getKey(Resource file)
                {
                    if (file.Id == accessPoint.AccessPoint.TargetFileId)
                    {
                        return KeyService.AesPair.Deserialize(
                            userAuthenticationToken.Decrypt(
                                accessPoint.AccessPoint.TargetFileAesKey
                            )
                        );
                    }

                    if (file.ParentId != null)
                    {
                        return await getKey(
                            (
                                await GetManager<FileManager>()
                                    .GetByRequiredId(transaction, (long)file.ParentId)
                            )!
                        );
                    }

                    return null;
                }

                return await getKey(file);
            }

            return null;
        }
    }

    public override Task<bool> Delete(ResourceService.Transaction transaction, Resource resource)
    {
        throw new InvalidAccessException(resource, null);
    }

    public async Task<bool> Delete(
        ResourceService.Transaction transaction,
        Resource resource,
        UserAuthenticationToken userAuthenticationToken
    )
    {
        if (resource.ParentId == null)
        {
            throw new InvalidRootDeleteException(resource);
        }

        Resource oldParent = await GetByRequiredId(transaction, (long)resource.ParentId);
        KeyService.AesPair oldParentKey = await GetKeyRequired(
            transaction,
            oldParent,
            FileAccessExtent.ReadWrite,
            userAuthenticationToken
        );
        if (resource.TrashTime != null)
        {
            Resource root = await GetRootFromUser(transaction, userAuthenticationToken);
            KeyService.AesPair rootKey = await GetKeyRequired(
                transaction,
                root,
                FileAccessExtent.ReadWrite,
                userAuthenticationToken
            );

            await Update(
                transaction,
                resource,
                new(
                    (COLUMN_AES_KEY, rootKey.Encrypt(oldParentKey.Decrypt(resource.AesKey))),
                    (COLUMN_PARENT_ID, root.Id)
                )
            );
        }

        return await base.Delete(transaction, resource);
    }
}
