import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { FileManager, type UnlockedFile } from './file';
import { FileContentManager, type FileContent } from './file-content';
import type { FileSnapshot } from './file-snapshot';
import { FileBufferManager, type FileBuffer } from './file-buffer';
import { FileType } from '$lib/shared/db';
import mmm, { Magic } from 'mmmagic';

const magic = new Magic();
const magicMime = new Magic(mmm.MAGIC_MIME_TYPE | mmm.MAGIC_MIME_ENCODING);

export interface FileData extends Data<FileDataManager, FileData> {
  [FileDataManager.KEY_FILE_ID]: number;
  [FileDataManager.KEY_FILE_CONTENT_ID]: number;
  [FileDataManager.KEY_FILE_SNAPSHOT_ID]: number;
  [FileDataManager.KEY_FILE_BUFFER_ID]: number;
  [FileDataManager.KEY_INDEX]: number;
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
    }
  }

  async #create(
    unlockedFile: UnlockedFile,
    fileContent: FileContent,
    fileSnapshot: FileSnapshot,
    buffer: Uint8Array,
    index: number
  ): Promise<FileData> {
    const [fileBuffers] = this.getManagers(FileBufferManager);
    const fileBuffer = await fileBuffers.create(unlockedFile, buffer);

    return this.insert({
      [FileDataManager.KEY_FILE_ID]: unlockedFile.id,
      [FileDataManager.KEY_FILE_CONTENT_ID]: fileContent.id,
      [FileDataManager.KEY_FILE_SNAPSHOT_ID]: fileSnapshot.id,
      [FileDataManager.KEY_FILE_BUFFER_ID]: fileBuffer.id,
      [FileDataManager.KEY_INDEX]: index
    });
  }

  async #update(unlockedFile: UnlockedFile, fileData: FileData, buffer: Uint8Array): Promise<void> {
    const [fileBuffers] = this.getManagers(FileBufferManager);

    const oldFileBuffer = await fileBuffers.getById(fileData[FileDataManager.KEY_FILE_BUFFER_ID]);
    const newFileBuffer = await fileBuffers.create(unlockedFile, buffer);

    await this.update(fileData, {
      [FileDataManager.KEY_FILE_BUFFER_ID]: newFileBuffer[FileBufferManager.KEY_ID]
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
    const positionEnd = position + newBuffer.byteLength;

    let written: number = 0;
    for (let index = 0; ; index++) {
      const bufferStart = index * FileDataManager.BUFFER_SIZE;
      const bufferEnd = bufferStart + FileDataManager.BUFFER_SIZE;

      if (positionEnd <= bufferStart) {
        break;
      } else if (position >= bufferEnd) {
        continue;
      }

      const fileData = await this.#getByIndex(unlockedFile, fileContent, fileSnapshot, index);
      let buffer: Uint8Array;
      if (fileData == null) {
        buffer = new Uint8Array(FileDataManager.BUFFER_SIZE);
      } else {
        const fileBuffer = fileBuffers.unlock(
          unlockedFile,
          (await fileBuffers.getById(fileData[FileDataManager.KEY_FILE_BUFFER_ID]))!
        );
        buffer = fileBuffer[FileBufferManager.KEY_UNLOCKED_BUFFER];
      }

      const toWrite = Math.min(
        bufferEnd - position,
        Math.min(newBuffer.byteLength - written, FileDataManager.BUFFER_SIZE)
      );
      buffer.set(
        newBuffer.subarray(written, written + toWrite),
        (bufferStart - position) % FileDataManager.BUFFER_SIZE
      );
      written += toWrite;

      if (fileData == null) {
        await this.#create(unlockedFile, fileContent, fileSnapshot, buffer, index);
      } else {
        await this.#update(unlockedFile, fileData, buffer);
      }
    }

    const [fileContentManager] = this.getManagers(FileContentManager);
    await fileContentManager.setSize(
      fileContent,
      Math.max(positionEnd, fileContent[FileContentManager.KEY_SIZE])
    );
  }

  public async read(
    unlockedFile: UnlockedFile,
    fileContent: FileContent,
    fileSnapshot: FileSnapshot,
    position: number = 0,
    length: number = fileContent[FileContentManager.KEY_SIZE]
  ): Promise<Uint8Array> {
    if (position >= fileContent[FileContentManager.KEY_SIZE]) {
      throw new Error('Position out of bounds');
    }

    const [fileBuffers] = this.getManagers(FileBufferManager);
    length = Math.min(length, fileContent[FileContentManager.KEY_SIZE] - position);
    const positionEnd = position + length;

    const output: Uint8Array[] = [];
    let read: number = 0;
    for (let index = 0; ; index++) {
      const bufferStart = index * FileDataManager.BUFFER_SIZE;
      const bufferEnd = bufferStart + FileDataManager.BUFFER_SIZE;

      if (positionEnd <= bufferStart) {
        break;
      } else if (position >= bufferEnd) {
        continue;
      }

      const fileData = await this.#getByIndex(unlockedFile, fileContent, fileSnapshot, index);
      let buffer: Uint8Array;

      if (fileData == null) {
        buffer = new Uint8Array(FileDataManager.BUFFER_SIZE);
      } else {
        const fileBuffer = fileBuffers.unlock(
          unlockedFile,
          (await fileBuffers.getById(fileData[FileDataManager.KEY_FILE_BUFFER_ID]))!
        );
        buffer = fileBuffer[FileBufferManager.KEY_UNLOCKED_BUFFER];
      }

      const toRead = Math.min(
        bufferEnd - position,
        Math.min(length - read, FileDataManager.BUFFER_SIZE)
      );
      output.push(
        buffer.subarray(
          (bufferStart - position) % FileDataManager.BUFFER_SIZE,
          ((bufferStart - position) % FileDataManager.BUFFER_SIZE) + toRead
        )
      );
      read += toRead;
    }

    return Buffer.concat(output);
  }

  public async getMime(
    unlockedFile: UnlockedFile,
    fileContent: FileContent,
    fileSnapshot: FileSnapshot,
    mime: boolean = true
  ): Promise<string> {
    mime ??=  true
    const [fileBufferManager] = this.getManagers(FileBufferManager);

    if (unlockedFile[FileManager.KEY_TYPE] === FileType.Folder) {
      return mime ? 'inode/directory' : 'Folder';
    }

    const fileData = await this.#getByIndex(unlockedFile, fileContent, fileSnapshot, 0);
    if (fileData == null) {
      return mime ? 'application/empty' : 'Empty File';
    } else {
      const buffer = fileBufferManager.unlock(
        unlockedFile,
        (await fileBufferManager.getById(fileData[FileDataManager.KEY_FILE_BUFFER_ID]))!
      );

      return await new Promise((resolve, reject) => {
        (mime ? magicMime : magic).detect(
          Buffer.from(buffer[FileBufferManager.KEY_UNLOCKED_BUFFER]),
          (err, result) => {
            if (err) {
              reject(err);
            } else {
              resolve(`${mime ? result : `${result}`.split(',')[0]}`);
            }
          }
        );
      });

      // return mmmagic.fromBuffer(buffer[FileBufferManager.KEY_UNLOCKED_BUFFER]).mime;
    }
  }
}

Database.register(FileDataManager);
