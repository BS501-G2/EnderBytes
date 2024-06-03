import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { FileType } from '$lib/shared/db';
import {
  decryptAsymmetric,
  decryptSymmetric,
  encryptSymmetric,
  generateKeyPair,
  randomBytes
} from '../utils';
import { UserKeyManager, type UnlockedUserKey } from './user-key';

export interface File extends Data<FileManager, File> {
  [FileManager.KEY_PARENT_FILE_ID]: number | null;
  [FileManager.KEY_CREATOR_USER_ID]: number;
  [FileManager.KEY_OWNER_USER_ID]: number;

  [FileManager.KEY_NAME]: string;
  [FileManager.KEY_TYPE]: FileType;

  [FileManager.KEY_ENCRYPTED_AES_KEY]: Uint8Array;
  [FileManager.KEY_ENCRYPTED_AES_KEY_IV]: Uint8Array;
  [FileManager.KEY_ENCRYPTED_AES_KEY_AUTH_TAG]: Uint8Array;
}

export function sanitizeFile(file: File): SanitizedFile {
  return Object.assign({}, file, {
    [FileManager.KEY_ENCRYPTED_AES_KEY]: undefined,
    [FileManager.KEY_ENCRYPTED_AES_KEY_IV]: undefined,
    [FileManager.KEY_ENCRYPTED_AES_KEY_AUTH_TAG]: undefined
  });
}

export type SanitizedFile = Omit<
  File,
  | typeof FileManager.KEY_ENCRYPTED_AES_KEY
  | typeof FileManager.KEY_ENCRYPTED_AES_KEY_IV
  | typeof FileManager.KEY_ENCRYPTED_AES_KEY_AUTH_TAG
>;

export interface UnlockedFile extends File {
  [FileManager.KEY_UNLOCKED_AES_KEY]: Uint8Array;
}

export class FileManager extends DataManager<FileManager, File> {
  public static readonly NAME = 'File';
  public static readonly VERSION = 1;

  public static readonly KEY_PARENT_FILE_ID = 'parentFileId';
  public static readonly KEY_OWNER_USER_ID = 'ownerUserId';
  public static readonly KEY_CREATOR_USER_ID = 'creatorUserId';

  public static readonly KEY_NAME = 'name';
  public static readonly KEY_TYPE = 'type';

  public static readonly KEY_ENCRYPTED_AES_KEY = 'encryptedAesKey';
  public static readonly KEY_ENCRYPTED_AES_KEY_IV = 'encryptedAesKeyIv';
  public static readonly KEY_ENCRYPTED_AES_KEY_AUTH_TAG = 'encryptedAesKeyAuthTag';

  public static readonly KEY_UNLOCKED_AES_KEY = 'aesKey';

  public constructor(db: Database, transaction: () => Knex.Transaction<File>) {
    super(db, transaction, FileManager.NAME, FileManager.VERSION);
  }

  protected upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): void {
    if (oldVersion < 1) {
      table.integer(FileManager.KEY_PARENT_FILE_ID).nullable();
      table.integer(FileManager.KEY_CREATOR_USER_ID).notNullable();
      table.integer(FileManager.KEY_OWNER_USER_ID).notNullable();

      table.string(FileManager.KEY_NAME).notNullable();
      table.integer(FileManager.KEY_TYPE).notNullable();

      table.binary(FileManager.KEY_ENCRYPTED_AES_KEY).notNullable();
      table.binary(FileManager.KEY_ENCRYPTED_AES_KEY_IV).notNullable();
      table.binary(FileManager.KEY_ENCRYPTED_AES_KEY_AUTH_TAG).notNullable();
    }
  }

  public get ftsColumns(): (keyof File)[] {
    return [FileManager.KEY_NAME];
  }

  public async create<T extends FileType>(
    unlockedUserKey: UnlockedUserKey,
    parent: (File & { [FileManager.KEY_TYPE]: FileType.Folder }) | null,
    name: string,
    type: T
  ): Promise<UnlockedFile & { [FileManager.KEY_TYPE]: T }> {
    if (parent == null) {
      const key = await randomBytes(32);
      const [userKeys] = this.getManagers(UserKeyManager);
      const encryptedAesKey = userKeys.encrypt(unlockedUserKey, key);

      const file = await this.insert({
        [FileManager.KEY_PARENT_FILE_ID]: null,
        [FileManager.KEY_CREATOR_USER_ID]: unlockedUserKey.userId,
        [FileManager.KEY_OWNER_USER_ID]: unlockedUserKey.userId,

        [FileManager.KEY_NAME]: name,
        [FileManager.KEY_TYPE]: type,

        [FileManager.KEY_ENCRYPTED_AES_KEY]: encryptedAesKey,
        [FileManager.KEY_ENCRYPTED_AES_KEY_IV]: new Uint8Array(0),
        [FileManager.KEY_ENCRYPTED_AES_KEY_AUTH_TAG]: new Uint8Array(0)
      });

      return { ...file, [FileManager.KEY_UNLOCKED_AES_KEY]: key } as UnlockedFile & {
        [FileManager.KEY_TYPE]: T;
      };
    } else {
      if (parent[FileManager.KEY_TYPE] !== FileType.Folder) {
        throw new Error('Parent is not a folder.');
      }

      const [key, iv] = await Promise.all([randomBytes(32), randomBytes(16)]);
      const unlockedParent = await this.unlock(parent, unlockedUserKey);

      const [authTag, encryptedKey] = encryptSymmetric(
        unlockedParent[FileManager.KEY_UNLOCKED_AES_KEY],
        iv,
        key
      );

      const file = await this.insert({
        [FileManager.KEY_PARENT_FILE_ID]: parent.id,
        [FileManager.KEY_CREATOR_USER_ID]: unlockedUserKey.userId,
        [FileManager.KEY_OWNER_USER_ID]: unlockedParent[FileManager.KEY_OWNER_USER_ID],

        [FileManager.KEY_NAME]: name,
        [FileManager.KEY_TYPE]: type,

        [FileManager.KEY_ENCRYPTED_AES_KEY]: encryptedKey,
        [FileManager.KEY_ENCRYPTED_AES_KEY_IV]: iv,
        [FileManager.KEY_ENCRYPTED_AES_KEY_AUTH_TAG]: authTag
      });

      await this.update(
        parent,
        {},
        {
          baseVersionId: parent[DataManager.KEY_DATA_VERSION_ID]
        }
      );

      return { ...file, [FileManager.KEY_UNLOCKED_AES_KEY]: key } as UnlockedFile & {
        [FileManager.KEY_TYPE]: T;
      };
    }
  }

  public async unlock(file: File, unlockedUserKey: UnlockedUserKey): Promise<UnlockedFile> {
    const [userKeys] = this.getManagers(UserKeyManager);
    const parentFileid = file[FileManager.KEY_PARENT_FILE_ID];

    if (parentFileid != null) {
      const parentFile = (await this.getById(parentFileid))!;
      const unlockedParentFile = await this.unlock(parentFile, unlockedUserKey);

      const key = decryptSymmetric(
        unlockedParentFile[FileManager.KEY_UNLOCKED_AES_KEY],
        file[FileManager.KEY_ENCRYPTED_AES_KEY_IV],
        file[FileManager.KEY_ENCRYPTED_AES_KEY],
        file[FileManager.KEY_ENCRYPTED_AES_KEY_AUTH_TAG]
      );

      return {
        ...file,
        [FileManager.KEY_UNLOCKED_AES_KEY]: key
      };
    } else {
      const key = userKeys.decrypt(unlockedUserKey, file[FileManager.KEY_ENCRYPTED_AES_KEY]);

      return {
        ...file,
        [FileManager.KEY_UNLOCKED_AES_KEY]: key
      };
    }
  }

  public async scanFolder(
    folder: File & { [FileManager.KEY_TYPE]: FileType.Folder }
  ): Promise<File[]> {
    const files = await this.query({
      where: [
        [FileManager.KEY_PARENT_FILE_ID, '=', folder.id],
        [FileManager.KEY_TYPE, '!=', FileType.Folder]
      ]
    });

    return files;
  }
}

Database.register(FileManager);
