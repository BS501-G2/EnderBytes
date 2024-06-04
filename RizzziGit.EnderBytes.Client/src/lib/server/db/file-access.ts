import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import type { FileAccessLevel } from '$lib/shared/db';
import { FileManager, type UnlockedFile } from './file';
import type { User } from './user';
import { randomBytes } from '../utils';
import { UserKeyManager, type UnlockedUserKey } from './user-key';

export interface FileAccess extends Data<FileAccessManager, FileAccess> {
  [FileAccessManager.KEY_FILE_ID]: number;
  [FileAccessManager.KEY_USER_ID]: number;
  [FileAccessManager.KEY_ACCESS_LEVEL]: FileAccessLevel;

  [FileAccessManager.KEY_ENCRYPTED_KEY]: Uint8Array;
}

export interface UnlockedFileAccess extends FileAccess {
  [FileAccessManager.KEY_UNLOCKED_KEY]: Uint8Array;
}

export class FileAccessManager extends DataManager<FileAccessManager, FileAccess> {
  public static readonly NAME = 'FileAccess';
  public static readonly VERSION = 1;

  public static readonly KEY_FILE_ID = 'fileId';
  public static readonly KEY_USER_ID = 'userId';
  public static readonly KEY_ACCESS_LEVEL = 'accessLevel';

  public static readonly KEY_ENCRYPTED_KEY = 'encryptedKey';

  public static readonly KEY_UNLOCKED_KEY = 'key';

  public constructor(db: Database, transaction: () => Knex.Transaction<FileAccess>) {
    super(db, transaction, FileAccessManager.NAME, FileAccessManager.VERSION);
  }

  protected get ftsColumns(): (keyof FileAccess)[] {
    return [];
  }

  protected upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): void {
    if (oldVersion < 1) {
      table.integer(FileAccessManager.KEY_FILE_ID).notNullable();
      table.integer(FileAccessManager.KEY_USER_ID).notNullable();
      table.integer(FileAccessManager.KEY_ACCESS_LEVEL).notNullable();
    }
  }

  public async create(
    unlockedFile: UnlockedFile,
    targetUser: User,
    level: FileAccessLevel
  ): Promise<UnlockedFileAccess> {
    const [userKeys] = this.getManagers(UserKeyManager);
    const userKey = (
      await userKeys.query({
        limit: 1,
        where: [[UserKeyManager.KEY_USER_ID, '=', targetUser.id]]
      })
    )[0]!;

    const fileKey = unlockedFile[FileManager.KEY_UNLOCKED_AES_KEY];

    const fileAccess = await this.insert({
      [FileAccessManager.KEY_FILE_ID]: unlockedFile.id,
      [FileAccessManager.KEY_USER_ID]: targetUser.id,
      [FileAccessManager.KEY_ACCESS_LEVEL]: level,
      [FileAccessManager.KEY_ENCRYPTED_KEY]: userKeys.encrypt(userKey, fileKey)
    });

    return {
      ...fileAccess,
      [FileAccessManager.KEY_UNLOCKED_KEY]: fileKey
    };
  }

  public unlock(unlockedUserKey: UnlockedUserKey, fileAccess: FileAccess): UnlockedFileAccess {
    const [userKeys] = this.getManagers(UserKeyManager);

    const unlockedKey = userKeys.decrypt(
      unlockedUserKey,
      fileAccess[FileAccessManager.KEY_ENCRYPTED_KEY]
    );

    return { ...fileAccess, [FileAccessManager.KEY_UNLOCKED_KEY]: unlockedKey };
  }
}

Database.register(FileAccessManager);
