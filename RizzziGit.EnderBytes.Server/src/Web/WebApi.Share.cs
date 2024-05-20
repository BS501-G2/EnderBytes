using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using Services;

public sealed partial class WebApi
{
    [Route("~/shares")]
    [HttpGet]
    public async Task<ObjectResult> GetAccessList(
        string sort = "ctime",
        bool desc = true,
        int offset = 0,
        long? authorUserId = null,
        long? domainUserId = null,
        int extent = 0
    )
    {
        return await Run(async () =>
        {
            FileAccessManager fileAccessManager = GetResourceManager<FileAccessManager>();

            List<FileAccessManager.Resource> fileAccesses = [];

            string? sortColumn = sort switch
            {
                "ctime" => ResourceService.ResourceManager.COLUMN_CREATE_TIME,
                "utime" => ResourceService.ResourceManager.COLUMN_UPDATE_TIME,

                _ => null
            };

            foreach (
                FileAccessManager.Resource fileAccess in await fileAccessManager
                    .List(
                        CurrentTransaction,
                        null,
                        CurrentUserAuthenticationToken.Required().User,
                        (FileAccessExtent)extent,
                        authorUserId != null
                            ? await GetResourceManager<UserManager>()
                                .GetByRequiredId(CurrentTransaction, (long)authorUserId)
                            : null,
                        null,
                        null,
                        new(100, offset),
                        sortColumn != null ? [new(sort, desc)] : null
                    )
            )
            {
                FileManager.Resource file = await GetResourceManager<FileManager>()
                    .GetByRequiredId(CurrentTransaction, fileAccess.TargetFileId);

                if ((domainUserId != file.DomainUserId) && fileAccesses.Count >= 25)
                {
                    continue;
                }

                fileAccesses.Add(fileAccess);
            }

            return Data(fileAccesses);
        });
    }

    public sealed record GetFileAccessListResponse(
        FileAccessExtent HighestExtent,
        FileAccessPoint? AccessPoint,
        FileAccessManager.Resource[] AccessList
    );

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
            FileAccessManager.Resource[] fileAccesses = await fileAccessManager
                .List(
                    CurrentTransaction,
                    file,
                    CurrentUserAuthenticationToken.Required().User,
                    null
                );

            if (file.DomainUserId == CurrentUserAuthenticationToken.Required().UserId)
            {
                return Data(
                    new GetFileAccessListResponse(FileAccessExtent.Full, null, fileAccesses)
                );
            }

            FileAccessPoint? fileAccessPoint = null;
            if (
                (
                    fileAccessPoint = await fileAccessManager.GetAccessPoint(
                        CurrentTransaction,
                        CurrentUserAuthenticationToken.Required().User,
                        file,
                        FileAccessExtent.None
                    )
                ) == null
            )
            {
                return Error(403);
            }

            FileAccessExtent highestExtent = fileAccesses
                .Select(fileAccess => fileAccess.Extent)
                .Append(fileAccessPoint.AccessPoint.Extent)
                .Max();

            return Data(new GetFileAccessListResponse(highestExtent, fileAccessPoint, list()));

            FileAccessManager.Resource[] list()
            {
                if (highestExtent >= FileAccessExtent.ManageAccess)
                {
                    return fileAccesses;
                }

                return fileAccesses
                    .Where(fileAccess =>
                        fileAccess.TargetEntityType == FileAccessTargetEntityType.User
                        && fileAccess.TargetEntityId
                            == CurrentUserAuthenticationToken.Required().UserId
                    )
                    .ToArray();
            }
        });
    }
}
