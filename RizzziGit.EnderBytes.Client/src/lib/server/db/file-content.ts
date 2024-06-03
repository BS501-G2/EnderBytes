import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import type { UnlockedFile } from './file';

export interface FileContent extends Data<FileContentManager, FileContent> {
  [FileContentManager.KEY_FILE_ID]: number;
  [FileContentManager.KEY_IS_MAIN]: boolean;
}

export class FileContentManager extends DataManager<FileContentManager, FileContent> {
  public static readonly NAME = 'FileContent';
  public static readonly VERSION = 1;

  public static readonly KEY_FILE_ID = 'fileId';
  public static readonly KEY_IS_MAIN = 'isMain';

  public constructor(db: Database, transaction: () => Knex.Transaction<FileContent>) {
    super(db, transaction, FileContentManager.NAME, FileContentManager.VERSION);
  }

  protected get ftsColumns(): (keyof FileContent)[] {
    return [];
  }

  protected upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): void {
    if (oldVersion < 1) {
      table.integer(FileContentManager.KEY_FILE_ID).notNullable();
      table.boolean(FileContentManager.KEY_IS_MAIN).notNullable();
    }
  }

  public async create(unlockedFile: UnlockedFile): Promise<FileContent> {
    return this.insert({
      [FileContentManager.KEY_FILE_ID]: unlockedFile.id,
      [FileContentManager.KEY_IS_MAIN]: (await this.queryCount()) === 0
    });
  }

  public async getMain(unlockedFile: UnlockedFile) {
    return (
      (
        await this.query({
          where: [
            [FileContentManager.KEY_FILE_ID, '=', unlockedFile.id],
            [FileContentManager.KEY_IS_MAIN, '=', true]
          ]
        })
      )[0] || (await this.create(unlockedFile))
    );
  }
}

Database.register(FileContentManager);
