using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using Services;

public partial class WebApi
{
  public sealed record GetFileAccessListResponse(FileAccessExtent HighestExtent, FileAccessManager.Resource[] AccessList);

  [Route("~/file/:{fileId}/access-list")]
  [Route("~/file/!root/access-list")]
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
        return Data(new GetFileAccessListResponse(FileAccessExtent.Full, fileAccesses));
      }
      else if (fileAccesses.Length == 0)
      {
        return Data(new GetFileAccessListResponse(FileAccessExtent.None, []));
      }

      FileAccessExtent highestExtent = fileAccesses
        .Select(fileAccess => fileAccess.FileAccessExtent)
        .Max();

      return Data(new GetFileAccessListResponse(
        highestExtent,
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

  // [Route("~/file/:{fileId}/contents")]
  // public async Task<ObjectResult> ListFileContents(long fileId, long contentId)
  // {
  //   ObjectResult fileResult = await GetFileById(fileId);
  //   if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
  //   {
  //     return fileResult;
  //   }

  //   return await Run(async () =>
  //   {
  //     FileContentManager fileContentManager = GetResourceManager<FileContentManager>();

  //     return Data(await fileContentManager.List(CurrentTransaction, file));
  //   });
  // }

  // [Route("~/file/:{fileId}/content")]
  // [HttpGet]
  // public async Task<ObjectResult> GetFileContentById(long fileId, long contentId, long versionId)
  // {
  //   ObjectResult fileResult = await GetFileById(fileId);
  //   if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
  //   {
  //     return fileResult;
  //   }

  //   return await Run(async () =>
  //   {
  //     FileManager fileManager = GetResourceManager<FileManager>();
  //     FileContentManager fileContentManager = GetResourceManager<FileContentManager>();
  //     FileContentVersionManager fileContentVersionManager = GetResourceManager<FileContentVersionManager>();
  //     FileDataManager fileDataManager = GetResourceManager<FileDataManager>();

  //     if (!file.IsFolder)
  //     {
  //       return Error(400);
  //     }
  //     else if (!await fileManager.TestAccess(CurrentTransaction, file, FileAccessExtent.ReadOnly, CurrentUserAuthenticationToken))
  //     {
  //       return Error(403);
  //     }
  //   });
  // }
}
