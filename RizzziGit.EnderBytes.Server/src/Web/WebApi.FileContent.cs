using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Resources;
using Services;

public partial class WebApi
{
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
