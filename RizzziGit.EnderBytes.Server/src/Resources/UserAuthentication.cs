using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace RizzziGit.EnderBytes.Resources;

using Services;
using Utilities;

public enum UserAuthenticationType
{
    Password,
    SessionToken
}

public sealed record UserAuthenticationToken(
    KeyService service,
    UserManager.Resource User,
    UserAuthenticationManager.Resource UserAuthentication,
    byte[] PayloadHash
)
{
    public long UserId => UserAuthentication.UserId;

    public byte[] Encrypt(byte[] bytes) => UserAuthentication.Encrypt(service, bytes);

    public byte[] Decrypt(byte[] bytes) => UserAuthentication.Decrypt(service, bytes, PayloadHash);
}

public sealed partial class UserAuthenticationManager
    : ResourceManager<UserAuthenticationManager, UserAuthenticationManager.Resource>
{
    public sealed class InvalidPayloadException() : Exception("Invalid payload specified.");

    public sealed class InvalidSessionTokenException()
        : Exception("Invalid session token specified.");

    public new sealed partial record Resource(
        long Id,
        long CreateTime,
        long UpdateTime,
        long UserId,
        UserAuthenticationType Type,
        byte[] Salt,
        int Iterations,
        byte[] ChallengeIv,
        byte[] ChallengeBytes,
        byte[] ChallengeEncryptedBytes,
        byte[] EncryptedPrivateKey,
        byte[] EncryptedPrivateKeyIv,
        byte[] PublicKey
    ) : ResourceManager<UserAuthenticationManager, Resource>.Resource(Id, CreateTime, UpdateTime)
    {
        public static byte[] AesEncrypt(byte[] key, byte[] iv, byte[] bytes)
        {
            using Aes aes = Aes.Create();
            using ICryptoTransform cryptoTransform = aes.CreateEncryptor(key, iv);

            return cryptoTransform.TransformFinalBlock(bytes);
        }

        public static byte[] AesDecrypt(byte[] key, byte[] iv, byte[] bytes)
        {
            using Aes aes = Aes.Create();
            using ICryptoTransform cryptoTransform = aes.CreateDecryptor(key, iv);

            return cryptoTransform.TransformFinalBlock(bytes);
        }

        private byte[]? PrivateKey;
        private RSACryptoServiceProvider? CryptoServiceProvider;
        private UserAuthenticationToken? TokenCache;

        ~Resource() => CryptoServiceProvider?.Dispose();

        public byte[] GetPrivateKey(byte[] payloadHash)
        {
            lock (this)
            {
                return PrivateKey ??= AesDecrypt(
                    payloadHash,
                    EncryptedPrivateKeyIv,
                    EncryptedPrivateKey
                );
            }
        }

        public byte[] GetPayloadHash(byte[] payload)
        {
            using Rfc2898DeriveBytes rfc2898DeriveBytes =
                new(payload, Salt, Iterations, HashAlgorithmName.SHA256);
            byte[] payloadHash = rfc2898DeriveBytes.GetBytes(32);

            if (
                !AesEncrypt(payloadHash, ChallengeIv, ChallengeBytes)
                    .SequenceEqual(ChallengeEncryptedBytes)
            )
            {
                throw new ArgumentException("Invalid payload.", nameof(payload));
            }

            return payloadHash;
        }

        public RSACryptoServiceProvider GetRSACryptoServiceProvider(
            KeyService service,
            byte[] cspBlob
        )
        {
            RSACryptoServiceProvider cryptoServiceProvider = service.GetRsaCryptoServiceProvider(
                cspBlob
            );

            return cryptoServiceProvider;
        }

        public byte[] Decrypt(KeyService service, byte[] bytes, byte[] payloadHash)
        {
            lock (this)
            {
                if (CryptoServiceProvider?.PublicOnly != false)
                {
                    byte[] privateKey = GetPrivateKey(payloadHash);

                    CryptoServiceProvider?.Dispose();
                    CryptoServiceProvider = GetRSACryptoServiceProvider(service, privateKey);
                }

                return CryptoServiceProvider.Decrypt(bytes, false);
            }
        }

        public byte[] Encrypt(KeyService service, byte[] bytes)
        {
            lock (this)
            {
                return (
                    CryptoServiceProvider ??= GetRSACryptoServiceProvider(service, PublicKey)
                ).Encrypt(bytes, false);
            }
        }

        public bool TryGetTokenByPayload(
            KeyService service,
            UserManager.Resource user,
            byte[] payload,
            [NotNullWhen(true)] out UserAuthenticationToken? userAuthenticationToken
        )
        {
            userAuthenticationToken = null;

            try
            {
                byte[] payloadHash = GetPayloadHash(payload);

                userAuthenticationToken = TokenCache ??= new(service, user, this, payloadHash);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    public const string NAME = "UserAuthentication";
    public const int VERSION = 1;

    public const string COLUMN_USER_ID = "UserId";
    public const string COLUMN_TYPE = "Type";

    public const string COLUMN_SALT = "Salt";
    public const string COLUMN_ITERATIONS = "Iterations";

    public const string COLUMN_CHALLENGE_IV = "ChallengeIv";
    public const string COLUMN_CHALLENGE_BYTES = "ChallengeBytes";
    public const string COLUMN_CHALLENGE_ENCRYPTED_BYTES = "ChallengeEncryptedBytes";

    public const string COLUMN_ENCRYPTED_PRIVATE_KEY = "EncryptedPrivateKey";
    public const string COLUMN_ENCRYPTED_PRIVATE_KEY_IV = "EncryptedPrivateKeyIv";
    public const string COLUMN_PUBLIC_KEY = "PublicKey";

    public const string INDEX_USER_ID = $"Index_{NAME}_{COLUMN_USER_ID}";

    public UserAuthenticationManager(ResourceService service)
        : base(service, NAME, VERSION)
    {
        service
            .GetManager<UserManager>()
            .RegisterDeleteHandler(
                (transaction, user) =>
                    Delete(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id))
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
            reader.GetInt64(reader.GetOrdinal(COLUMN_USER_ID)),
            (UserAuthenticationType)reader.GetByte(reader.GetOrdinal(COLUMN_TYPE)),
            reader.GetBytes(reader.GetOrdinal(COLUMN_SALT)),
            reader.GetInt32(reader.GetOrdinal(COLUMN_ITERATIONS)),
            reader.GetBytes(reader.GetOrdinal(COLUMN_CHALLENGE_IV)),
            reader.GetBytes(reader.GetOrdinal(COLUMN_CHALLENGE_BYTES)),
            reader.GetBytes(reader.GetOrdinal(COLUMN_CHALLENGE_ENCRYPTED_BYTES)),
            reader.GetBytes(reader.GetOrdinal(COLUMN_ENCRYPTED_PRIVATE_KEY)),
            reader.GetBytes(reader.GetOrdinal(COLUMN_ENCRYPTED_PRIVATE_KEY_IV)),
            reader.GetBytes(reader.GetOrdinal(COLUMN_PUBLIC_KEY))
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
                $"alter table {NAME} add column {COLUMN_USER_ID} integer not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_TYPE} integer not null;"
            );

            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_SALT} blob not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_ITERATIONS} integer not null;"
            );

            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_CHALLENGE_IV} blob not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_CHALLENGE_BYTES} blob not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_CHALLENGE_ENCRYPTED_BYTES} blob not null;"
            );

            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_ENCRYPTED_PRIVATE_KEY} blob not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_ENCRYPTED_PRIVATE_KEY_IV} blob not null;"
            );
            await SqlNonQuery(
                transaction,
                $"alter table {NAME} add column {COLUMN_PUBLIC_KEY} blob not null;"
            );

            await SqlNonQuery(
                transaction,
                $"create index {INDEX_USER_ID} on {NAME}({COLUMN_USER_ID});"
            );
        }
    }

    public async Task<UserAuthenticationToken> CreatePassword(
        ResourceService.Transaction transaction,
        UserManager.Resource user,
        UserAuthenticationToken existing,
        string password
    ) =>
        await Create(
            transaction,
            user,
            existing,
            UserAuthenticationType.Password,
            Encoding.UTF8.GetBytes(password)
        );

    public async Task<UserAuthenticationToken> CreatePassword(
        ResourceService.Transaction transaction,
        UserManager.Resource user,
        string password,
        byte[] privateKey,
        byte[] publicKey
    ) =>
        await Create(
            transaction,
            user,
            UserAuthenticationType.Password,
            Encoding.UTF8.GetBytes(password),
            privateKey,
            publicKey
        );

    public async Task<string> CreateSessionToken(
        ResourceService.Transaction transaction,
        UserManager.Resource user,
        UserAuthenticationToken baseToken
    )
    {
        byte[] sessionToken = RandomNumberGenerator.GetBytes(64);

        UserAuthenticationToken userAuthenticationToken = await Create(
            transaction,
            user,
            baseToken,
            UserAuthenticationType.SessionToken,
            sessionToken
        );
        await transaction
            .GetManager<UserAuthenticationSessionTokenManager>()
            .Create(transaction, userAuthenticationToken.UserAuthentication, 36000 * 1000);

        return Convert.ToHexString(sessionToken);
    }

    public async Task<bool> TruncateSessionToken(
        ResourceService.Transaction transaction,
        UserManager.Resource user
    )
    {
        await foreach (
            Resource userAuthentication in Select(
                    transaction,
                    new WhereClause.Nested(
                        "and",
                        new WhereClause.CompareColumn(
                            COLUMN_TYPE,
                            "=",
                            (byte)UserAuthenticationType.SessionToken
                        ),
                        new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id)
                    ),
                    order: [new OrderByClause(COLUMN_CREATE_TIME, true)]
                )
                .Skip(10)
        )
        {
            await Delete(transaction, userAuthentication);
        }

        return true;
    }

    public async Task<UserAuthenticationToken?> GetSessionToken(
        ResourceService.Transaction transaction,
        UserManager.Resource user,
        string sessionToken
    )
    {
        UserAuthenticationToken? userAuthenticationToken = null;

        await foreach (
            Resource userAuthentication in Select(
                transaction,
                new WhereClause.Nested(
                    "and",
                    new WhereClause.CompareColumn(
                        COLUMN_TYPE,
                        "=",
                        (byte)UserAuthenticationType.SessionToken
                    ),
                    new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id)
                )
            )
        )
        {
            if (
                (
                    await transaction
                        .GetManager<UserAuthenticationSessionTokenManager>()
                        .GetByUserAuthentication(transaction, userAuthentication)
                ).Expired
            )
            {
                await Delete(transaction, userAuthentication);

                continue;
            }

            if (
                userAuthentication.TryGetTokenByPayload(
                    Service.Server.KeyService,
                    user,
                    Convert.FromHexString(sessionToken),
                    out userAuthenticationToken
                )
            )
            {
                break;
            }
        }

        if (userAuthenticationToken != null)
        {
            UserAuthenticationSessionTokenManager.Resource userAuthenticationSessionToken =
                await Service
                    .GetManager<UserAuthenticationSessionTokenManager>()
                    .GetByUserAuthentication(
                        transaction,
                        userAuthenticationToken.UserAuthentication
                    );

            if (userAuthenticationSessionToken.Expired)
            {
                userAuthenticationToken = null;
            }
        }

        if (userAuthenticationToken == null)
        {
            return null;
        }

        return userAuthenticationToken;
    }

    private async Task<UserAuthenticationToken> Create(
        ResourceService.Transaction transaction,
        UserManager.Resource user,
        UserAuthenticationToken existing,
        UserAuthenticationType type,
        byte[] payload
    )
    {
        byte[] privateKey = existing.UserAuthentication.GetPrivateKey(existing.PayloadHash);
        byte[] publicKey = existing.UserAuthentication.PublicKey;

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        int iterations = RandomNumberGenerator.GetInt32(1000, 10000);
        byte[] payloadHash;
        {
            using Rfc2898DeriveBytes rfc2898DeriveBytes =
                new(payload, salt, iterations, HashAlgorithmName.SHA256);
            payloadHash = rfc2898DeriveBytes.GetBytes(32);
        }

        byte[] challengeIv = RandomNumberGenerator.GetBytes(16);
        byte[] challengeBytes = RandomNumberGenerator.GetBytes(16);
        byte[] challengeEncryptedBytes = Resource.AesEncrypt(
            payloadHash,
            challengeIv,
            challengeBytes
        );

        byte[] encryptedPrivateKeyIv = RandomNumberGenerator.GetBytes(16);
        byte[] encryptedPrivateKey = Resource.AesEncrypt(
            payloadHash,
            encryptedPrivateKeyIv,
            privateKey
        );

        return new(
            Service.Server.KeyService,
            user,
            await InsertAndGet(
                transaction,
                new(
                    (COLUMN_USER_ID, user.Id),
                    (COLUMN_TYPE, (byte)type),
                    (COLUMN_SALT, salt),
                    (COLUMN_ITERATIONS, iterations),
                    (COLUMN_CHALLENGE_IV, challengeIv),
                    (COLUMN_CHALLENGE_BYTES, challengeBytes),
                    (COLUMN_CHALLENGE_ENCRYPTED_BYTES, challengeEncryptedBytes),
                    (COLUMN_ENCRYPTED_PRIVATE_KEY, encryptedPrivateKey),
                    (COLUMN_ENCRYPTED_PRIVATE_KEY_IV, encryptedPrivateKeyIv),
                    (COLUMN_PUBLIC_KEY, publicKey)
                )
            ),
            payloadHash
        );
    }

    private async Task<UserAuthenticationToken> Create(
        ResourceService.Transaction transaction,
        UserManager.Resource user,
        UserAuthenticationType type,
        byte[] payload,
        byte[] privateKey,
        byte[] publicKey
    )
    {
        if (
            await Count(transaction, new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id))
            != 0
        )
        {
            throw new InvalidOperationException("Must use an existing rsa key.");
        }

        byte[] salt = RandomNumberGenerator.GetBytes(16);
        int iterations = RandomNumberGenerator.GetInt32(1000, 10000);
        byte[] payloadHash;
        {
            using Rfc2898DeriveBytes rfc2898DeriveBytes =
                new(payload, salt, iterations, HashAlgorithmName.SHA256);
            payloadHash = rfc2898DeriveBytes.GetBytes(32);
        }

        byte[] challengeIv = RandomNumberGenerator.GetBytes(16);
        byte[] challengeBytes = RandomNumberGenerator.GetBytes(64);
        byte[] challengeEncryptedBytes = Resource.AesEncrypt(
            payloadHash,
            challengeIv,
            challengeBytes
        );

        byte[] encryptedPrivateKeyIv = RandomNumberGenerator.GetBytes(16);
        byte[] encryptedPrivateKey = Resource.AesEncrypt(
            payloadHash,
            encryptedPrivateKeyIv,
            privateKey
        );

        return new(
            Service.Server.KeyService,
            user,
            await InsertAndGet(
                transaction,
                new(
                    (COLUMN_USER_ID, user.Id),
                    (COLUMN_TYPE, (byte)type),
                    (COLUMN_SALT, salt),
                    (COLUMN_ITERATIONS, iterations),
                    (COLUMN_CHALLENGE_IV, challengeIv),
                    (COLUMN_CHALLENGE_BYTES, challengeBytes),
                    (COLUMN_CHALLENGE_ENCRYPTED_BYTES, challengeEncryptedBytes),
                    (COLUMN_ENCRYPTED_PRIVATE_KEY, encryptedPrivateKey),
                    (COLUMN_ENCRYPTED_PRIVATE_KEY_IV, encryptedPrivateKeyIv),
                    (COLUMN_PUBLIC_KEY, publicKey)
                )
            ),
            payloadHash
        );
    }

    public IAsyncEnumerable<Resource> List(
        ResourceService.Transaction transaction,
        UserManager.Resource user,
        LimitClause? limitClause = null,
        OrderByClause[]? orderByClause = null
    ) =>
        Select(
            transaction,
            new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id),
            limitClause,
            orderByClause
        );

    public async Task<UserAuthenticationToken?> GetByPayload(
        ResourceService.Transaction transaction,
        UserManager.Resource user,
        byte[] payload,
        UserAuthenticationType type
    )
    {
        UserAuthenticationToken? userAuthenticationToken = null;

        await foreach (
            Resource userAuthentication in Select(
                transaction,
                new WhereClause.Nested(
                    "and",
                    new WhereClause.CompareColumn(COLUMN_USER_ID, "=", user.Id),
                    new WhereClause.CompareColumn(COLUMN_TYPE, "=", (byte)type)
                )
            )
        )
        {
            try
            {
                userAuthenticationToken = new(
                    Service.Server.KeyService,
                    user,
                    userAuthentication,
                    userAuthentication.GetPayloadHash(payload)
                );
                break;
            }
            catch { }
        }

        return userAuthenticationToken;
    }

    public override async Task<bool> Delete(
        ResourceService.Transaction transaction,
        Resource userAuthentication
    )
    {
        if (
            await Count(
                transaction,
                new WhereClause.CompareColumn(COLUMN_USER_ID, "=", userAuthentication.UserId)
            ) < 2
        )
        {
            throw new InvalidOperationException(
                "Must have at least two user authentications before deleting one."
            );
        }

        return await base.Delete(transaction, userAuthentication);
    }
}
