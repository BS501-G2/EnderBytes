using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using RizzziGit.EnderBytes.Services;

public sealed partial class WebApi
{
  public sealed record CreateFileAccessRequest(FileAccessExtent Extent, long UserId);

  // [Route("~/file/:{fileId}/shares")]
  // [Route("~/file/!root/shares")]
  // [HttpPost]
  // public async Task<ObjectResult> CreateFileAccess(long? fileId, [FromBody] CreateFileAccessRequest request)
  // {
  //   ObjectResult fileResult = await GetFileById(fileId);
  //   if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
  //   {
  //     return fileResult;
  //   }

  //   return await Run(async () =>
  //   {
  //     FileAccessManager fileAccessManager = GetResourceManager<FileAccessManager>();
  //     FileManager fileManager = GetResourceManager<FileManager>();
  //     UserManager userManager = GetResourceManager<UserManager>();

  //     UserManager.Resource? user = null;
  //     if ((user = await userManager.GetById(CurrentTransaction, request.UserId)) == null)
  //     {
  //       return Error(400, "User does not exist.");
  //     }

  //     KeyService.AesPair? fileKey = await fileManager.GetKey(CurrentTransaction, file, FileAccessExtent.ManageAccess, CurrentUserAuthenticationToken.Required());
  //     if (fileKey == null)
  //     {
  //       return Error(403);
  //     }

  //     if (file.DomainUserId == CurrentUserAuthenticationToken.Required().UserId)
  //     {
  //       await fileAccessManager.GrantUser(CurrentTransaction, file, user, fileKey, request.Extent);
  //     }
  //   });
  // }

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
      FileAccessManager.Resource[] fileAccesses = await fileAccessManager.List(CurrentTransaction, file, CurrentUserAuthenticationToken.Required().User, null);

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
