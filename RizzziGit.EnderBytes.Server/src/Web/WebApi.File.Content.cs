using Microsoft.AspNetCore.Mvc;

namespace RizzziGit.EnderBytes.Web;

using System.Threading;
using Resources;

public sealed partial class WebApi
{
    [HttpGet]
    [Route("~/file/!root/content")]
    [Route("~/file/:{fileId}/content")]
    public async Task<ActionResult> GetFileContentList(long? fileId)
    {
        ObjectResult fileResult = await GetFileById(fileId);
        if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
        {
            return fileResult;
        }
        return await Run(async () =>
        {
            if (file.IsFolder)
            {
                return Error(404);
            }

            FileContentManager fileContentManager = GetResourceManager<FileContentManager>();
            FileContentManager.Resource[] list = await fileContentManager.List(
                CurrentTransaction,
                file
            );

            return Data(list);
        });
    }

    public WebApiContext.InstanceHolder<FileContentManager.Resource> CurrentFileContent =>
        new(this, nameof(CurrentFileContent));

    [HttpGet]
    [Route("~/file/:{fileId}/content/:{fileContentId}")]
    [Route("~/file/:{fileId}/content/!main")]
    public async Task<ObjectResult> GetFileContentById(long fileId, long? fileContentId)
    {
        ObjectResult fileResult = await GetFileById(fileId);
        if (!TryGetValueFromResult(fileResult, out FileManager.Resource? file))
        {
            return fileResult;
        }

        return await Run(async () =>
        {
            FileContentManager fileContentManager = GetResourceManager<FileContentManager>();
            FileContentManager.Resource? fileContent =
                fileContentId == null
                    ? await fileContentManager.GetMainContent(CurrentTransaction, file)
                    : await fileContentManager.GetById(CurrentTransaction, (long)fileContentId);

            if (fileContent == null || fileContent.FileId != file.Id)
            {
                return Error(404);
            }

            return Data(CurrentFileContent.Set(fileContent));
        });
    }

    [HttpGet]
    [Route("~/file/:{fileId}/content/:{fileContentId}/version")]
    [Route("~/file/:{fileId}/content/!main/version")]
    public async Task<ObjectResult> GetFileContentVersions(long fileId, long? fileContentId)
    {
        ObjectResult fileResult = await GetFileContentById(fileId, fileContentId);
        if (!TryGetValueFromResult(fileResult, out FileContentManager.Resource? fileContent))
        {
            return fileResult;
        }

        return await Run(async () =>
        {
            FileContentManager fileContentManager = GetResourceManager<FileContentManager>();
            FileContentVersionManager fileContentVersionManager =
                GetResourceManager<FileContentVersionManager>();

            return Data(await fileContentVersionManager.List(CurrentTransaction, fileContent));
        });
    }

    [HttpGet]
    [Route("~/file/:{fileId}/content/:{fileContentId}/version/:{fileContentVersionId}")]
    [Route("~/file/:{fileId}/content/:{fileContentId}/version/!latest")]
    [Route("~/file/:{fileId}/content/!main/version/:{fileContentVersionId}")]
    [Route("~/file/:{fileId}/content/!main/version/!latest")]
    public async Task<ObjectResult> GetFileContentVersionById(
        long fileId,
        long? fileContentId,
        long? fileContentVersionId
    )
    {
        ObjectResult fileResult = await GetFileContentById(fileId, fileContentId);
        if (!TryGetValueFromResult(fileResult, out FileContentManager.Resource? fileContent))
        {
            return fileResult;
        }

        return await Run(async () =>
        {
            FileContentVersionManager fileContentVersionManager =
                GetResourceManager<FileContentVersionManager>();

            FileContentVersionManager.Resource? fileContentVersion =
                fileContentVersionId == null
                    ? await fileContentVersionManager.GetLatestVersion(
                        CurrentTransaction,
                        fileContent
                    )
                    : await fileContentVersionManager.GetById(
                        CurrentTransaction,
                        (long)fileContentVersionId
                    );

            if (fileContentVersion == null)
            {
                return Error(404);
            }

            return Data(fileContentVersion);
        });
    }

    private readonly Dictionary<long, FileContentDataStream> Streams = [];

    [HttpGet]
    [Route("~/file/:{fileId}/content/:{fileContentId}/version/:{fileContentVersionId}/data")]
    [Route("~/file/:{fileId}/content/:{fileContentId}/version/!latest/data")]
    [Route("~/file/:{fileId}/content/!main/version/:{fileContentVersionId}/data")]
    [Route("~/file/:{fileId}/content/!main/version/!latest/data")]
    public async Task<ObjectResult> GetFileContentData(
        long fileId,
        long? fileContentId,
        long? fileContentVersionId
    )
    {
        ObjectResult fileResult = await GetFileContentVersionById(
            fileId,
            fileContentId,
            fileContentVersionId
        );

        if (
            !TryGetValueFromResult(
                fileResult,
                out FileContentVersionManager.Resource? fileContentVersion
            )
        )
        {
            return fileResult;
        }

        return await Run(async () =>
        {
            return Error(404);

            FileContentVersionManager fileContentVersionManager =
                GetResourceManager<FileContentVersionManager>();
            FileContentManager fileContentManager = GetResourceManager<FileContentManager>();
            FileDataManager fileDataManager = GetResourceManager<FileDataManager>();

            while (true)
            {
                long size = await fileDataManager.GetSize(
                    CurrentTransaction,
                    CurrentFile,
                    CurrentFileContent,
                    fileContentVersion
                );

                FileContentDataStream a =
                    new(this, fileId, fileContentId ?? 0, fileContentVersion.Id, size);

                lock (Streams)
                {
                    Streams.TryAdd(fileContentVersion.Id, a);
                }
            }
        });
    }

    private class FileContentDataStream(
        WebApi api,
        long fileId,
        long fileContentId,
        long fileContentVersionId,
        long size
    ) : Stream
    {
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                api.Streams.Remove(fileContentVersionId);
            }
        }

        public override ValueTask DisposeAsync()
        {
            Dispose(true);
            return ValueTask.CompletedTask;
        }

        public override bool CanRead => true;
        public override bool CanSeek => true;
        public override bool CanWrite => true;

        public override long Length => size;

        private long PositionBackingField = 0;
        public override long Position
        {
            get => PositionBackingField;
            set
            {
                if (value < 0 || value > size)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                PositionBackingField = value;
            }
        }

        public override void Flush() { }

        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) =>
            Position = origin switch
            {
                SeekOrigin.Begin => offset,
                SeekOrigin.Current => Position + offset,
                SeekOrigin.End => size + offset,
                _ => throw new ArgumentOutOfRangeException(nameof(origin))
            };

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException();

        public override IAsyncResult BeginWrite(
            byte[] buffer,
            int offset,
            int count,
            AsyncCallback? callback,
            object? state
        )
        {
            return base.BeginWrite(buffer, offset, count, callback, state);
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            return 0;
        }

        public override async Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        ) => await base.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
    }
}
