using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using Commons.Memory;
using Resources;
using Services;

public partial class WebApi
{
    public WebApiContext.InstanceHolder<FileManager.Resource> CurrentFile =>
        new(this, nameof(CurrentFile));

    [Route("~/file/!root")]
    [Route("~/file/:{fileId}")]
    [HttpGet]
    public Task<ObjectResult> GetFileById(long? fileId) =>
        Run(async () =>
        {
            if (
                !TryGetUserAuthenticationToken(out UserAuthenticationToken? userAuthenticationToken)
            )
            {
                return Error(401);
            }

            FileManager fileManager = GetResourceManager<FileManager>();
            FileAccessManager fileAccessManager = GetResourceManager<FileAccessManager>();

            FileManager.Resource? file = null;
            if (fileId == null)
            {
                file = await fileManager.GetRootFromUser(
                    CurrentTransaction,
                    userAuthenticationToken
                );
            }
            else if ((file = await fileManager.GetById(CurrentTransaction, (long)fileId)) == null)
            {
                return Error(404);
            }
            else if (
                !await fileManager.TestAccess(
                    CurrentTransaction,
                    file,
                    FileAccessExtent.ReadOnly,
                    userAuthenticationToken
                )
            )
            {
                return Error(403);
            }

            CurrentFile.Set(file);

            return Data(file);
        });

    public sealed record GetPathChainResponse(
        FileManager.Resource Root,
        FileManager.Resource[] Chain,
        bool IsSharePoint
    );

    [Route("~/file/:{fileId}/path-chain")]
    [Route("~/file/!root/path-chain")]
    [HttpGet]
    public async Task<ObjectResult> GetFilePathChainById(long? fileId)
    {
        ObjectResult fileResult = await GetFileById(fileId);
        if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
        {
            return fileResult;
        }

        return await Run(async () =>
        {
            FileManager fileManager = GetResourceManager<FileManager>();
            FileAccessManager fileAccessManager = GetResourceManager<FileAccessManager>();

            if (file.DomainUserId != CurrentUserAuthenticationToken.Required().UserId)
            {
                FileAccessPoint? fileAccessPoint = await fileAccessManager.GetAccessPoint(
                    CurrentTransaction,
                    CurrentUserAuthenticationToken.Required().User,
                    file,
                    FileAccessExtent.ReadOnly
                );

                if (fileAccessPoint == null)
                {
                    return Error(403);
                }

                FileManager.Resource fileAccessPointRoot = await fileManager.GetByRequiredId(
                    CurrentTransaction,
                    fileAccessPoint.AccessPoint.TargetFileId
                );
                return Data(
                    new GetPathChainResponse(fileAccessPointRoot, fileAccessPoint.PathChain, true)
                );
            }

            return Data(
                new GetPathChainResponse(
                    await fileManager.GetRootFromUser(
                        CurrentTransaction,
                        CurrentUserAuthenticationToken
                    ),
                    await fileManager.PathChain(CurrentTransaction, file),
                    false
                )
            );
        });
    }

    [Route("~/file/!root/files")]
    [Route("~/file/:{fileId}/files")]
    [HttpGet]
    public async Task<ObjectResult> ScanFolder(
        long? fileId,
        string sort = "name",
        bool desc = false,
        int offset = 0
    )
    {
        ObjectResult fileResult = await GetFileById(fileId);
        if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
        {
            return fileResult;
        }

        return await Run(async () =>
        {
            if (!file.IsFolder)
            {
                return Error(400);
            }

            FileManager fileManager = GetResourceManager<FileManager>();

            string? sortColumn = sort switch
            {
                "name" => FileManager.COLUMN_NAME,
                "ctime" => ResourceService.ResourceManager.COLUMN_CREATE_TIME,
                "utime" => ResourceService.ResourceManager.COLUMN_UPDATE_TIME,

                _ => null
            };

            if (sortColumn == null)
            {
                return Error(400);
            }

            FileManager.Resource[] files = (
                await fileManager.ScanFolder(
                    CurrentTransaction,
                    file,
                    CurrentUserAuthenticationToken,
                    new(100, offset),
                    [
                        new(FileManager.COLUMN_IS_FOLDER, true),
                        sortColumn != null ? new(sortColumn, desc) : null
                    ]
                )
            );

            return Data(files);
        });
    }

    public sealed record NewFileNameValidationRequest(string Name);

    public sealed record NewFileNameValidationResponse(
        bool HasIllegalCharacters,
        bool HasIllegalLength,
        bool NameInUse
    );

    [Route("/file/sanitize-name")]
    [HttpPost]
    public async Task<ObjectResult> SanitizeFileName(string name)
    {
        return await Run(
            () =>
                Task.FromResult<Result>(
                    Data(
                        Path.GetInvalidFileNameChars()
                            .Aggregate(name, (name, character) => name.Replace(character, '_'))
                            .ToString()
                    )
                )
        );
    }

    [Route("~/file/!root/files/new-name-validation")]
    [Route("~/file/:{fileId}/files/new-name-validation")]
    [HttpPost]
    public async Task<ObjectResult> NewFileNameValidation(
        long? fileId,
        [FromBody] NewFileNameValidationRequest request
    )
    {
        ObjectResult fileResult = await GetFileById(fileId);
        if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
        {
            return fileResult;
        }

        return await Run(async () =>
        {
            FileNameVaildationFlag fileNameVaildationFlag = await GetResourceManager<FileManager>()
                .ValidateName(CurrentTransaction, file, request.Name);

            return Data<NewFileNameValidationResponse>(
                new(
                    fileNameVaildationFlag.HasFlag(FileNameVaildationFlag.HasIllegalCharacters),
                    fileNameVaildationFlag.HasFlag(FileNameVaildationFlag.HasIllegalLength),
                    fileNameVaildationFlag.HasFlag(FileNameVaildationFlag.NameInUse)
                )
            );
        });
    }

    public sealed record CreateFileRequest(string Name, IFormFile Content);

    public sealed record CreateFileResponse(FileManager.Resource File);

    [Route("~/file/!root/files/new-file")]
    [Route("~/file/:{fileId}/files/new-file")]
    [HttpPost]
    public async Task<ActionResult> CreateFile(long? fileId, [FromForm] CreateFileRequest request)
    {
        ObjectResult fileResult = await GetFileById(fileId);
        if (!TryGetValueFromResult(fileResult, out FileManager.Resource? parentFolder))
        {
            return fileResult;
        }

        return await Run(async () =>
        {
            FileManager fileManager = GetResourceManager<FileManager>();
            FileContentManager fileContentManager = GetResourceManager<FileContentManager>();
            FileContentVersionManager fileContentVersionManager =
                GetResourceManager<FileContentVersionManager>();
            FileDataManager fileDataManager = GetResourceManager<FileDataManager>();

            if (!parentFolder.IsFolder)
            {
                return Error(400);
            }
            else if (
                !await fileManager.TestAccess(
                    CurrentTransaction,
                    parentFolder,
                    FileAccessExtent.ReadWrite,
                    CurrentUserAuthenticationToken
                )
            )
            {
                return Error(403);
            }

            (FileManager.Resource file, KeyService.AesPair fileKey) = await fileManager.Create(
                CurrentTransaction,
                parentFolder,
                request.Name,
                false,
                CurrentUserAuthenticationToken
            );
            FileContentManager.Resource fileContent = await fileContentManager.GetMainContent(
                CurrentTransaction,
                file
            );
            FileContentVersionManager.Resource fileContentVersion =
                await fileContentVersionManager.GetBaseVersion(CurrentTransaction, fileContent);
            {
                await using Stream fileStream = request.Content.OpenReadStream();
                while (fileStream.Position < fileStream.Length)
                {
                    byte[] buffer = new byte[FileDataManager.BUFFER_SIZE / 2];
                    int bufferLength = await fileStream.ReadAsync(buffer);

                    await fileDataManager.Write(
                        CurrentTransaction,
                        file,
                        fileKey,
                        fileContent,
                        fileContentVersion,
                        CompositeBuffer.From(buffer, 0, bufferLength),
                        fileStream.Position - bufferLength
                    );
                }
            }

            return Data(file);
        });
    }

    public sealed record CreateFolderRequest(string Name);

    [Route("~/file/!root/files/new-folder")]
    [Route("~/file/:{fileId}/files/new-folder")]
    [HttpPost]
    public async Task<ActionResult> CreateFolder(
        long? fileId,
        [FromBody] CreateFolderRequest request
    )
    {
        ObjectResult fileResult = await GetFileById(fileId);
        if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
        {
            return fileResult;
        }

        return await Run(async () =>
        {
            FileManager fileManager = GetResourceManager<FileManager>();

            if (!file.IsFolder)
            {
                return Error(400);
            }
            else if (
                !await fileManager.TestAccess(
                    CurrentTransaction,
                    file,
                    FileAccessExtent.ReadWrite,
                    CurrentUserAuthenticationToken
                )
            )
            {
                return Error(403);
            }

            return Data(
                (
                    await fileManager.Create(
                        CurrentTransaction,
                        file,
                        request.Name,
                        true,
                        CurrentUserAuthenticationToken
                    )
                ).File
            );
        });
    }
}
