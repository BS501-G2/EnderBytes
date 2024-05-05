using System.Data.Common;
using Newtonsoft.Json;

namespace RizzziGit.EnderBytes.Resources;

using Commons.Memory;

using Utilities;
using Services;

using ResourceManager = ResourceManager<FileBlobManager, FileBlobManager.Resource>;

public sealed class FileBlobManager : ResourceManager
{
	public new sealed record Resource(
		long Id,
		long CreateTime,
		long UpdateTime,

		byte[] Buffer
	) : ResourceManager.Resource(Id, CreateTime, UpdateTime);

	public const string NAME = "FileBlob";
	public const int VERSION = 1;

	public const string COLUMN_BUFFER = "Buffer";

	public FileBlobManager(ResourceService service) : base(service, NAME, VERSION)
	{
		GetManager<FileDataManager>().RegisterDeleteHandler(handler);
		GetManager<FileDataManager>().RegisterUpdateHandler((transaction, _, old) => handler(transaction, old));

		async Task handler(ResourceService.Transaction transaction, FileDataManager.Resource fileData)
		{
			long referenceCount = await GetManager<FileDataManager>().GetReferenceCount(transaction, fileData.BlobId);
			if (referenceCount == 0)
			{
				await SqlNonQuery(transaction, $"delete from {NAME} where {COLUMN_ID} = {fileData.Id}");
			}
		}
	}

	protected override Resource ToResource(DbDataReader reader, long id, long createTime, long updateTime) => new(
	id, createTime, updateTime,

	reader.GetBytes(reader.GetOrdinal(COLUMN_BUFFER))
	);

	protected override async Task Upgrade(ResourceService.Transaction transaction, int oldVersion = 0)
	{
		if (oldVersion < 1)
		{
			await SqlNonQuery(transaction, $"alter table {NAME} add column {COLUMN_BUFFER} mediumblob not null;");
		}
	}

	public async Task<long> Store(ResourceService.Transaction transaction, KeyService.AesPair fileKey, byte[] buffer)
	{
		return await Insert(transaction, new(
			(COLUMN_BUFFER, fileKey.Encrypt(buffer))
		));
	}

	public async Task<byte[]> Retrieve(ResourceService.Transaction transaction, KeyService.AesPair fileKey, long blobId)
	{
		Resource fileBlob = await GetByRequiredId(transaction, blobId);

		return fileKey.Decrypt(fileBlob.Buffer);
	}
}
