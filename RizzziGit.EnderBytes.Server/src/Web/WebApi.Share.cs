using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using Services;

public sealed partial class WebApi
{
	public sealed record GetAccessListRequest(
		FileAccessExtent? Extent,
		long? AuthorUserId,
		long? DomainUserId,
		long? FromCreatedAt,
		long? ToCreatedAt,
		long? AfterId
	);

	[Route("~/shares")]
	[HttpGet]
	public async Task<ObjectResult> GetAccessList([FromBody] GetAccessListRequest? request)
	{
		return await Run(async () =>
		{
			FileAccessManager fileAccessManager = GetResourceManager<FileAccessManager>();

			FileAccessExtent extent = request?.Extent ?? FileAccessExtent.None;
			UserManager.Resource? authorUser = request?.AuthorUserId != null ? await GetResourceManager<UserManager>().GetByRequiredId(CurrentTransaction, (long)request.AuthorUserId) : null;

			if (request?.FromCreatedAt != null)
			{
				extent = FileAccessExtent.ReadWrite;
			}

			List<FileAccessManager.Resource> fileAccesses = [];
			foreach (FileAccessManager.Resource fileAccess in await fileAccessManager.List(
				CurrentTransaction,
				null,
				CurrentUserAuthenticationToken.Required().User,
				extent,
				request?.AuthorUserId != null ? await GetResourceManager<UserManager>().GetByRequiredId(CurrentTransaction, (long)request.AuthorUserId) : null,
				request?.FromCreatedAt,
				request?.ToCreatedAt,
				null,
				orderBy: [
					new FileAccessManager.OrderByClause(ResourceService.ResourceManager.COLUMN_CREATE_TIME, FileAccessManager.OrderByClause.OrderBy.Descending)
				]
			).ToArrayAsync())
			{
				FileManager.Resource file = await GetResourceManager<FileManager>().GetByRequiredId(CurrentTransaction, fileAccess.TargetFileId);

				if (
					(request != null && request.DomainUserId != file.DomainUserId) ||
					(request?.AfterId != null && fileAccess.Id <= request.AfterId) ||
					fileAccesses.Count >= 100
				)
				{
					continue;
				}

				fileAccesses.Add(fileAccess);
			}

			return Data(fileAccesses);
		});
	}

	public sealed record GetFileAccessListResponse(FileAccessExtent HighestExtent, FileAccessPoint? AccessPoint, FileAccessManager.Resource[] AccessList);

	[Route("~/file/:{fileId}/shares")]
	[Route("~/file/!root/shares")]
	[HttpGet]
	public async Task<ObjectResult> GetFileAccessList(long? fileId)
	{
		ObjectResult fileResult = await GetFileById(fileId);
		if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
		{
			return fileResult;
		}

		return await Run(async () =>
		{
			FileAccessManager fileAccessManager = GetResourceManager<FileAccessManager>();
			FileAccessManager.Resource[] fileAccesses = await fileAccessManager.List(CurrentTransaction, file, CurrentUserAuthenticationToken.Required().User, null).ToArrayAsync();

			if (file.DomainUserId == CurrentUserAuthenticationToken.Required().UserId)
			{
				return Data(new GetFileAccessListResponse(FileAccessExtent.Full, null, fileAccesses));
			}

			FileAccessPoint? fileAccessPoint = null;
			if ((fileAccessPoint = await fileAccessManager.GetAccessPoint(CurrentTransaction, CurrentUserAuthenticationToken.Required().User, file, FileAccessExtent.None)) == null)
			{
				return Error(403);
			}

			FileAccessExtent highestExtent = fileAccesses
			.Select(fileAccess => fileAccess.FileAccessExtent)
			.Append(fileAccessPoint.AccessPoint.FileAccessExtent)
			.Max();

			return Data(new GetFileAccessListResponse(
				highestExtent,
				fileAccessPoint,
				list()
			));

			FileAccessManager.Resource[] list()
			{
				if (highestExtent >= FileAccessExtent.ManageAccess)
				{
					return fileAccesses;
				}

				return fileAccesses
					.Where(fileAccess => fileAccess.TargetEntityType == FileAccessTargetEntityType.User && fileAccess.TargetEntityId == CurrentUserAuthenticationToken.Required().UserId)
					.ToArray();
			}
		});
	}
}
