import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { FileManager, type UnlockedFile } from './file';
import type { FileContent } from './file-content';
import type { FileSnapshot } from './file-snapshot';
import { FileBufferManager, type FileBuffer } from './file-buffer';

export interface FileData extends Data<FileDataManager, FileData> {
  [FileDataManager.KEY_FILE_ID]: number;
  [FileDataManager.KEY_FILE_CONTENT_ID]: number;
  [FileDataManager.KEY_FILE_SNAPSHOT_ID]: number;
  [FileDataManager.KEY_FILE_BUFFER_ID]: number;
  [FileDataManager.KEY_INDEX]: number;
  [FileDataManager.KEY_SIZE]: number;
}

export class FileDataManager extends DataManager<FileDataManager, FileData> {
  static readonly BUFFER_SIZE = 1024 * 64;

  static readonly NAME = 'FileData';
  static readonly VERSION = 1;

  static readonly KEY_FILE_ID = 'fileId';
  static readonly KEY_FILE_CONTENT_ID = 'fileContentId';
  static readonly KEY_FILE_SNAPSHOT_ID = 'fileSnapshotId';
  static readonly KEY_FILE_BUFFER_ID = 'fileBufferId';
  static readonly KEY_INDEX = 'index';
  static readonly KEY_SIZE = 'fileSize';

  public constructor(db: Database, transaction: () => Knex.Transaction<FileData>) {
    super(db, transaction, FileDataManager.NAME, FileDataManager.VERSION);
  }

  protected get ftsColumns(): (keyof FileData)[] {
    return [];
  }

  protected upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): void {
    if (oldVersion < 1) {
      table.integer(FileDataManager.KEY_FILE_ID).notNullable();
      table.integer(FileDataManager.KEY_FILE_CONTENT_ID).notNullable();
      table.integer(FileDataManager.KEY_FILE_SNAPSHOT_ID).notNullable();
      table.integer(FileDataManager.KEY_FILE_BUFFER_ID).notNullable();
      table.integer(FileDataManager.KEY_INDEX).notNullable();
      table.integer(FileDataManager.KEY_SIZE).notNullable();
    }
  }

  async #create(
    unlockedFile: UnlockedFile,
    fileContent: FileContent,
    fileSnapshot: FileSnapshot,
    buffer: Uint8Array,
    index: number,
    size: number
  ): Promise<FileData> {
    const [fileBuffers] = this.getManagers(FileBufferManager);
    const fileBuffer = await fileBuffers.create(unlockedFile, buffer);

    return this.insert({
      [FileDataManager.KEY_FILE_ID]: unlockedFile.id,
      [FileDataManager.KEY_FILE_CONTENT_ID]: fileContent.id,
      [FileDataManager.KEY_FILE_SNAPSHOT_ID]: fileSnapshot.id,
      [FileDataManager.KEY_FILE_BUFFER_ID]: fileBuffer.id,
      [FileDataManager.KEY_INDEX]: index,
      [FileDataManager.KEY_SIZE]: size
    });
  }

  async #update(
    unlockedFile: UnlockedFile,
    fileData: FileData,
    buffer: Uint8Array,
    size: number
  ): Promise<void> {
    const [fileBuffers] = this.getManagers(FileBufferManager);

    const oldFileBuffer = await fileBuffers.getById(fileData[FileDataManager.KEY_FILE_BUFFER_ID]);
    const newFileBuffer = await fileBuffers.create(unlockedFile, buffer);

    await this.update(fileData, {
      [FileDataManager.KEY_FILE_BUFFER_ID]: newFileBuffer[FileBufferManager.KEY_ID],
      [FileDataManager.KEY_SIZE]: size
    });

    if (oldFileBuffer != null) {
      await this.#autoPurgeBuffer(oldFileBuffer);
    }
  }

  async #getByIndex(
    unlockedFile: UnlockedFile,
    fileContent: FileContent,
    fileSnapshot: FileSnapshot,
    index: number
  ): Promise<FileData | null> {
    return (
      (
        await this.query({
          where: [
            [FileDataManager.KEY_FILE_ID, '=', unlockedFile.id],
            [FileDataManager.KEY_FILE_CONTENT_ID, '=', fileContent.id],
            [FileDataManager.KEY_FILE_SNAPSHOT_ID, '=', fileSnapshot.id],
            [FileDataManager.KEY_INDEX, '=', index]
          ]
        })
      )[0] ?? null
    );
  }

  async #delete(fileData: FileData): Promise<void> {
    const [fileBuffers] = this.getManagers(FileBufferManager);

    const oldFileBuffer = await fileBuffers.getById(fileData[FileDataManager.KEY_FILE_BUFFER_ID]);

    await this.delete(fileData);

    if (oldFileBuffer != null) {
      await this.#autoPurgeBuffer(oldFileBuffer);
    }
  }

  async #autoPurgeBuffer(oldFileBuffer: FileBuffer): Promise<void> {
    const [fileBuffers] = this.getManagers(FileBufferManager);

    if (
      (await this.queryCount([
        [FileDataManager.KEY_FILE_BUFFER_ID, '=', oldFileBuffer[FileBufferManager.KEY_ID]],
        [DataManager.KEY_DATA_NEXT_ID, 'is', null]
      ])) === 0
    ) {
      await fileBuffers.purge(oldFileBuffer[FileBufferManager.KEY_ID]);
    }
  }

  public async write(
    unlockedFile: UnlockedFile,
    fileContent: FileContent,
    fileSnapshot: FileSnapshot,
    position: number,
    newBuffer: Uint8Array
  ) {
    const [fileBuffers] = this.getManagers(FileBufferManager);

    const endPosition = position + newBuffer.byteLength;
    for (let index = 0; ; index++) {
      const beginOffset = index * FileDataManager.BUFFER_SIZE;
      const endOffset = beginOffset + FileDataManager.BUFFER_SIZE;

      if (endPosition <= beginOffset) {
        break;
      } else if (position >= endOffset) {
        continue;
      }

      const fileData = await this.#getByIndex(unlockedFile, fileContent, fileSnapshot, index);
      if (fileData == null) {
        const buffer = new Uint8Array(FileDataManager.BUFFER_SIZE);
        const bufferStart = position - beginOffset;
        const bufferEnd = Math.max(Math.min(endPosition, endOffset) - beginOffset, 0);
        buffer.set(newBuffer.slice(bufferStart, bufferEnd), 0);
        await this.#create(unlockedFile, fileContent, fileSnapshot, buffer, index, bufferEnd);
      } else {
        const fileBuffer = fileBuffers.unlock(
          unlockedFile,
          (await fileBuffers.getById(fileData[FileDataManager.KEY_FILE_BUFFER_ID]))!
        );

        const buffer = fileBuffer[FileBufferManager.KEY_UNLOCKED_BUFFER];
        const bufferStart = position - beginOffset;
        const bufferEnd = Math.max(
          Math.min(endPosition, endOffset) - beginOffset,
          fileData[FileDataManager.KEY_SIZE]
        );
        buffer.set(newBuffer.slice(bufferStart, bufferEnd), 0);
        await this.#update(unlockedFile, fileData, buffer, bufferEnd);
      }
    }
  }
}

Database.register(FileDataManager);
