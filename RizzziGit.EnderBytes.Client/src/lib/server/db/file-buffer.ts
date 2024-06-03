import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { FileManager, type UnlockedFile } from './file';
import { decryptSymmetric, encryptSymmetric, randomBytes } from '../utils';

export interface FileBuffer extends Data<FileBufferManager, FileBuffer> {
  [FileBufferManager.KEY_ENCRYPTED_BUFFER]: Uint8Array;
  [FileBufferManager.KEY_ENCRYPTED_BUFFER_IV]: Uint8Array;
  [FileBufferManager.KEY_ENCRYPTED_BUFFER_AUTH_TAG]: Uint8Array;
}

export interface UnlockedFileBuffer extends FileBuffer {
  [FileBufferManager.KEY_UNLOCKED_BUFFER]: Uint8Array;
}

export class FileBufferManager extends DataManager<FileBufferManager, FileBuffer> {
  public static readonly NAME = 'FileBuffer';
  public static readonly VERSION = 1;

  public static readonly KEY_ENCRYPTED_BUFFER = 'buffer';
  public static readonly KEY_ENCRYPTED_BUFFER_IV = 'bufferIv';
  public static readonly KEY_ENCRYPTED_BUFFER_AUTH_TAG = 'bufferAuthTag';

  public static readonly KEY_UNLOCKED_BUFFER = 'unlockedBuffer';

  public constructor(db: Database, transaction: () => Knex.Transaction<FileBuffer>) {
    super(db, transaction, FileBufferManager.NAME, FileBufferManager.VERSION);
  }

  protected get ftsColumns(): (keyof FileBuffer)[] {
    return [];
  }

  protected upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): void {
    if (oldVersion < 1) {
      table.binary(FileBufferManager.KEY_ENCRYPTED_BUFFER).notNullable();
      table.binary(FileBufferManager.KEY_ENCRYPTED_BUFFER_IV).notNullable();
      table.binary(FileBufferManager.KEY_ENCRYPTED_BUFFER_AUTH_TAG).notNullable();
    }
  }

  public async create(file: UnlockedFile, buffer: Uint8Array): Promise<FileBuffer> {
    const iv = await randomBytes(16);
    const [authTag, encryptedBuffer] = encryptSymmetric(
      file[FileManager.KEY_UNLOCKED_AES_KEY],
      iv,
      buffer
    );

    return this.insert({
      [FileBufferManager.KEY_ENCRYPTED_BUFFER]: encryptedBuffer,
      [FileBufferManager.KEY_ENCRYPTED_BUFFER_IV]: iv,
      [FileBufferManager.KEY_ENCRYPTED_BUFFER_AUTH_TAG]: authTag
    });
  }

  public unlock(file: UnlockedFile, fileBuffer: FileBuffer): UnlockedFileBuffer {
    const buffer = decryptSymmetric(
      file[FileManager.KEY_UNLOCKED_AES_KEY],
      fileBuffer[FileBufferManager.KEY_ENCRYPTED_BUFFER_IV],
      fileBuffer[FileBufferManager.KEY_ENCRYPTED_BUFFER],
      fileBuffer[FileBufferManager.KEY_ENCRYPTED_BUFFER_AUTH_TAG]
    );

    return {
      ...fileBuffer,
      [FileBufferManager.KEY_UNLOCKED_BUFFER]: buffer
    };
  }
}

Database.register(FileBufferManager);
