import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import type { UnlockedFile } from './file';
import { FileContentManager, type FileContent } from './file-content';

export interface FileSnapshot extends Data<FileSnapshotManager, FileSnapshot> {
  [FileSnapshotManager.KEY_FILE_ID]: number;
  [FileSnapshotManager.KEY_FILE_CONTENT_ID]: number;
  [FileSnapshotManager.KEY_BASE_FILE_SNAPSHOT_ID]: number | null;
}

export class FileSnapshotManager extends DataManager<FileSnapshotManager, FileSnapshot> {
  static readonly NAME = 'FileSnapshot';
  static readonly VERSION = 1;

  static readonly KEY_FILE_ID = 'fileId';
  static readonly KEY_FILE_CONTENT_ID = 'fileContentId';
  static readonly KEY_BASE_FILE_SNAPSHOT_ID = 'baseFileSnapshotId';

  public constructor(db: Database, transaction: () => Knex.Transaction<FileSnapshot>) {
    super(db, transaction, FileSnapshotManager.NAME, FileSnapshotManager.VERSION);
  }

  protected get ftsColumns(): (keyof FileSnapshot)[] {
    return [];
  }

  protected upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): void {
    if (oldVersion < 1) {
      table.integer(FileSnapshotManager.KEY_FILE_ID).notNullable();
      table.integer(FileSnapshotManager.KEY_FILE_CONTENT_ID).notNullable();
      table.integer(FileSnapshotManager.KEY_BASE_FILE_SNAPSHOT_ID).nullable();
    }
  }

  public async create(
    unlockedFile: UnlockedFile,
    fileContent: FileContent,
    baseFileSnapshot: FileSnapshot
  ): Promise<FileSnapshot> {
    return this.insert({
      [FileSnapshotManager.KEY_FILE_ID]: unlockedFile.id,
      [FileSnapshotManager.KEY_FILE_CONTENT_ID]: fileContent.id,
      [FileSnapshotManager.KEY_BASE_FILE_SNAPSHOT_ID]: baseFileSnapshot[FileSnapshotManager.KEY_ID]
    });
  }

  public async getMain(
    unlockedFile: UnlockedFile,
    fileContent: FileContent
  ): Promise<FileSnapshot> {
    return (
      (
        await this.query({
          where: [
            [FileSnapshotManager.KEY_FILE_ID, '=', unlockedFile.id],
            [FileSnapshotManager.KEY_FILE_CONTENT_ID, '=', fileContent.id],
            [FileSnapshotManager.KEY_BASE_FILE_SNAPSHOT_ID, 'is', null]
          ]
        })
      )[0] ??
      (await this.insert({
        [FileSnapshotManager.KEY_FILE_ID]: unlockedFile.id,
        [FileSnapshotManager.KEY_FILE_CONTENT_ID]: fileContent.id,
        [FileSnapshotManager.KEY_BASE_FILE_SNAPSHOT_ID]: null
      }))
    );
  }

  public async list(fileContent: FileContent): Promise<FileSnapshot[]> {
    return await this.query({
      where: [
        [FileSnapshotManager.KEY_FILE_CONTENT_ID, '=', fileContent[FileContentManager.KEY_ID]]
      ]
    });
  }
}

Database.register(FileSnapshotManager);
