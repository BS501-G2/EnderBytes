using System.Data.Common;
using Newtonsoft.Json;

namespace RizzziGit.EnderBytes.Resources;

using Services;
using Utilities;
using ResourceManager = ResourceManager<FileAccessManager, FileAccessManager.Resource>;

public enum FileAccessTargetEntityType
{
    User,
    None
}

public enum FileAccessExtent
{
    None,
    ReadOnly,
    ReadWrite,
    ManageAccess,
    Full
}

public sealed record FileAccessPoint(
    FileAccessManager.Resource AccessPoint,
    FileManager.Resource[] PathChain
);

public sealed class FileAccessManager : ResourceManager
{
    public new sealed record Resource(
        long Id,
        long CreateTime,
        long UpdateTime,
        long AuthorUserId,
        FileAccessTargetEntityType TargetEntityType,
        long? TargetEntityId,
        long TargetFileId,
        byte[] TargetFileAesKey,
        FileAccessExtent Extent
    ) : ResourceManager.Resource(Id, CreateTime, UpdateTime)
    {
        [JsonIgnore]
        public byte[] TargetFileAesKey = TargetFileAesKey;
    }

    public const string NAME = "FileAccess";
    public const int VERSION = 1;

    private const string COLUMN_AUTHOR_USER_ID = "AuthorUserId";
    private const string COLUMN_TARGET_ENTITY_TYPE = "TargetEntityType";
    private const string COLUMN_TARGET_ENTITY_ID = "TargetEntityId";
    private const string COLUMN_TARGET_FILE_ID = "TargetFileId";
    private const string COLUMN_TARGET_FILE_AES_KEY = "TargetFileAesKey";
    private const string COLUMN_EXTENT = "Extent";

    public FileAccessManager(ResourceService service)
        : base(service, NAME, VERSION)
    {
        GetManager<UserManager>()
            .RegisterDeleteHandler(
                (transaction, user) =>
                    Delete(
                        transaction,
                        new WhereClause.Nested(
                            "and",
                            new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_ID, "=", user.Id),
                            new WhereClause.CompareColumn(
                                COLUMN_TARGET_ENTITY_TYPE,
                                "=",
                                FileAccessTargetEntityType.User
                            )
                        )
                    )
            );

        GetManager<FileManager>()
            .RegisterDeleteHandler(
                (transaction, file) =>
                    Delete(
                        transaction,
                        new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", file.Id)
                    )
            );
    }

    public KeyService KeyService => Service.Server.KeyService;

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
            reader.GetInt64(reader.GetOrdinal(COLUMN_AUTHOR_USER_ID)),
            (FileAccessTargetEntityType)
                reader.GetByte(reader.GetOrdinal(COLUMN_TARGET_ENTITY_TYPE)),
            reader.GetInt64Optional(reader.GetOrdinal(COLUMN_TARGET_ENTITY_ID)),
            reader.GetInt64(reader.GetOrdinal(COLUMN_TARGET_FILE_ID)),
            reader.GetBytes(reader.GetOrdinal(COLUMN_TARGET_FILE_AES_KEY)),
            (FileAccessExtent)reader.GetByte(reader.GetOrdinal(COLUMN_EXTENT))
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
                $"alter table {NAME} add column {COLUMN_AUTHOR_USER_ID} integer not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_TARGET_ENTITY_TYPE} integer not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_TARGET_ENTITY_ID} integer null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_TARGET_FILE_ID} integer not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_TARGET_FILE_AES_KEY} blob not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_EXTENT} integer not null;"
            );
        }
    }

    public async Task<Resource> GrantUser(
        ResourceService.Transaction transaction,
        FileManager.Resource file,
        UserManager.Resource targetUser,
        KeyService.AesPair fileKey,
        UserManager.Resource authorUser,
        FileAccessExtent extent = FileAccessExtent.ReadOnly
    )
    {
        return await InsertAndGet(
            transaction,
            new(
                (COLUMN_AUTHOR_USER_ID, authorUser.Id),
                (COLUMN_TARGET_ENTITY_TYPE, (byte)FileAccessTargetEntityType.User),
                (COLUMN_TARGET_ENTITY_ID, targetUser.Id),
                (COLUMN_TARGET_FILE_ID, file.Id),
                (COLUMN_TARGET_FILE_AES_KEY, targetUser.Encrypt(KeyService, fileKey.Serialize())),
                (COLUMN_EXTENT, (byte)extent)
            )
        );
    }

    public Task<Resource[]> List(
        ResourceService.Transaction transaction,
        FileManager.Resource? file = null,
        UserManager.Resource? targetUser = null,
        FileAccessExtent? extent = null,
        UserManager.Resource? authorUser = null,
        long? fromCreateTime = null,
        long? toCreateTime = null,
        LimitClause? limit = null,
        OrderByClause[]? orderBy = null
    )
    {
        return Select(
            transaction,
            new WhereClause.Nested(
                "and",
                file != null
                    ? new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", file.Id)
                    : null,
                fromCreateTime != null
                    ? new WhereClause.CompareColumn(COLUMN_CREATE_TIME, ">=", fromCreateTime)
                    : null,
                toCreateTime != null
                    ? new WhereClause.CompareColumn(COLUMN_CREATE_TIME, "<=", toCreateTime)
                    : null,
                targetUser != null
                    ? new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_ID, "=", targetUser.Id)
                    : null,
                targetUser != null
                    ? new WhereClause.CompareColumn(
                        COLUMN_TARGET_ENTITY_TYPE,
                        "=",
                        FileAccessTargetEntityType.User
                    )
                    : null,
                authorUser != null
                    ? new WhereClause.CompareColumn(COLUMN_AUTHOR_USER_ID, "=", authorUser.Id)
                    : null,
                extent == null
                    ? null
                    : new WhereClause.CompareColumn(COLUMN_EXTENT, ">=", (byte)extent)
            ),
            limit,
            orderBy ?? [new OrderByClause(COLUMN_EXTENT, true)]
        );
    }

    public async Task<FileAccessPoint?> GetAccessPoint(
        ResourceService.Transaction transaction,
        UserManager.Resource user,
        FileManager.Resource file,
        FileAccessExtent extent
    )
    {
        List<FileManager.Resource> pathChain = [];

        async Task<FileAccessPoint?> getAccessPoint(FileManager.Resource file)
        {
            Resource? accessPoint = await SelectFirst(
                transaction,
                new WhereClause.Nested(
                    "and",
                    new WhereClause.CompareColumn(
                        COLUMN_TARGET_ENTITY_TYPE,
                        "=",
                        (byte)FileAccessTargetEntityType.User
                    ),
                    new WhereClause.CompareColumn(COLUMN_TARGET_ENTITY_ID, "=", user.Id),
                    new WhereClause.CompareColumn(COLUMN_TARGET_FILE_ID, "=", file.Id),
                    new WhereClause.CompareColumn(COLUMN_EXTENT, ">=", (byte)extent)
                ),
                [new OrderByClause(COLUMN_EXTENT, true)]
            );

            if (accessPoint != null)
            {
                return new(accessPoint, [.. pathChain]);
            }
            else if (file.ParentId != null)
            {
                pathChain.Insert(0, file);

                return await getAccessPoint(
                    await GetManager<FileManager>()
                        .GetByRequiredId(transaction, (long)file.ParentId)
                );
            }

            return null;
        }

        return await getAccessPoint(file);
    }
}
