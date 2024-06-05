import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import {
  FileAccessLevel,
  FileNameVerificationFlag,
  FileType,
  fileNameInvalidCharacters,
  fileNameLength
} from '$lib/shared/db';
import { decryptSymmetric, encryptSymmetric, randomBytes } from '../utils';
import { UserKeyManager, type UnlockedUserKey } from './user-key';
import { FileAccessManager } from './file-access';

export interface File extends Data<FileManager, File> {
  [FileManager.KEY_PARENT_FILE_ID]: number | null;
  [FileManager.KEY_CREATOR_USER_ID]: number;
  [FileManager.KEY_OWNER_USER_ID]: number;

  [FileManager.KEY_NAME]: string;
  [FileManager.KEY_TYPE]: FileType;

  [FileManager.KEY_DELETED]: boolean;

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
  public static readonly KEY_DELETED = 'deleted';

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

      table.boolean(FileManager.KEY_DELETED).notNullable();

      table.binary(FileManager.KEY_ENCRYPTED_AES_KEY).notNullable();
      table.binary(FileManager.KEY_ENCRYPTED_AES_KEY_IV).notNullable();
      table.binary(FileManager.KEY_ENCRYPTED_AES_KEY_AUTH_TAG).notNullable();
    }
  }

  public get ftsColumns(): (keyof File)[] {
    return [FileManager.KEY_NAME];
  }

  public async verifiyFileName(
    parentFolder: File | null,
    fileName: string
  ): Promise<FileNameVerificationFlag> {
    let flag = FileNameVerificationFlag.OK;

    const [min, max] = fileNameLength;

    if (fileName.length > max || fileName.length < min) {
      flag |= FileNameVerificationFlag.InvalidLength;
    }

    if (fileName.match(`[${fileNameInvalidCharacters}]`)) {
      flag |= FileNameVerificationFlag.InvalidCharacters;
    }

    return flag;
  }

  public async create<T extends FileType>(
    unlockedUserKey: UnlockedUserKey,
    parent: UnlockedFile,
    name: string,
    type: T
  ): Promise<UnlockedFile> {
    if (parent == null) {
      throw new Error('Parent is null');
    }

    let fnCount = 1;
    const friendlyName = () => (fnCount === 1 ? name : `${name} (${fnCount})`);
    while (
      (await this.scanFolder(parent)).find(
        (entry) => entry[FileManager.KEY_NAME].toLowerCase() === friendlyName().toLowerCase()
      ) != null
    ) {
      if (type === FileType.Folder) {
        throw Error('Folder already exists.');
      }

      fnCount++;
    }

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

      [FileManager.KEY_NAME]: friendlyName(),
      [FileManager.KEY_TYPE]: type,
      [FileManager.KEY_DELETED]: false,

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

  public async getRoot(unlockedUserKey: UnlockedUserKey): Promise<File> {
    const root = (
      await this.query({
        where: [
          [FileManager.KEY_PARENT_FILE_ID, 'is', null],
          [FileManager.KEY_OWNER_USER_ID, '=', unlockedUserKey.userId]
        ],
        limit: 1
      })
    )[0]!;

    if (root != null) {
      return root;
    }

    const key = await randomBytes(32);
    const [userKeys] = this.getManagers(UserKeyManager);
    const encryptedAesKey = userKeys.encrypt(unlockedUserKey, key);

    const newRoot = await this.insert({
      [FileManager.KEY_PARENT_FILE_ID]: null,
      [FileManager.KEY_CREATOR_USER_ID]: unlockedUserKey.userId,
      [FileManager.KEY_OWNER_USER_ID]: unlockedUserKey.userId,

      [FileManager.KEY_NAME]: 'root',
      [FileManager.KEY_TYPE]: FileType.Folder,

      [FileManager.KEY_DELETED]: false,

      [FileManager.KEY_ENCRYPTED_AES_KEY]: encryptedAesKey,
      [FileManager.KEY_ENCRYPTED_AES_KEY_IV]: new Uint8Array(0),
      [FileManager.KEY_ENCRYPTED_AES_KEY_AUTH_TAG]: new Uint8Array(0)
    });

    return (await this.getById(newRoot[FileManager.KEY_ID]))!;
  }

  public async unlock(
    file: File,
    unlockedUserKey: UnlockedUserKey,
    accessLevel?: FileAccessLevel
  ): Promise<UnlockedFile> {
    const [userKeys] = this.getManagers(UserKeyManager);
    const parentFileid = file[FileManager.KEY_PARENT_FILE_ID];

    if (parentFileid != null) {
      if (file[FileManager.KEY_OWNER_USER_ID] !== unlockedUserKey[UserKeyManager.KEY_USER_ID]) {
        if (accessLevel == null) {
          throw new Error('Access level is required if not the owner of the file.');
        }

        const [fileAccesses] = this.getManagers(FileAccessManager);

        const fileAccess = (
          await fileAccesses.query({
            where: [
              [FileAccessManager.KEY_FILE_ID, '=', file.id],
              [FileAccessManager.KEY_USER_ID, '=', unlockedUserKey.userId],
              [FileAccessManager.KEY_ACCESS_LEVEL, '>=', accessLevel]
            ],
            orderBy: [[FileAccessManager.KEY_ACCESS_LEVEL, true]]
          })
        )[0];

        if (fileAccess != null) {
          const unlockedFileAccess = fileAccesses.unlock(unlockedUserKey, fileAccess);

          return {
            ...file,
            [FileManager.KEY_UNLOCKED_AES_KEY]:
              unlockedFileAccess[FileAccessManager.KEY_UNLOCKED_KEY]
          };
        }
      }

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
      if (file[FileManager.KEY_OWNER_USER_ID] !== unlockedUserKey[UserKeyManager.KEY_USER_ID]) {
        throw new Error('User does not have access to the file.');
      }

      const key = userKeys.decrypt(unlockedUserKey, file[FileManager.KEY_ENCRYPTED_AES_KEY]);

      return {
        ...file,
        [FileManager.KEY_UNLOCKED_AES_KEY]: key
      };
    }
  }

  public async scanFolder(folder: File): Promise<File[]> {
    const files = await this.query({
      where: [
        [FileManager.KEY_PARENT_FILE_ID, '=', folder.id],
        [FileManager.KEY_DELETED, '=', false]
      ],
      orderBy: [[FileManager.KEY_TYPE, true]]
    });

    return files;
  }

  public async move(file: UnlockedFile, newParent: UnlockedFile) {
    const files = await this.scanFolder(newParent);

    if (file[FileManager.KEY_PARENT_FILE_ID] == null) {
      throw new Error('Cannot move  root folder');
    }

    if (files.find((f) => f[FileManager.KEY_NAME] === file[FileManager.KEY_NAME])) {
      throw new Error('Destination has conflicting file');
    }

    const checkForParent = async (folder: File) => {
      if (folder[FileManager.KEY_ID] == file[FileManager.KEY_ID]) {
        throw new Error('Cannot move a folder into itself');
      } else {
        const parentId = folder[FileManager.KEY_PARENT_FILE_ID];
        if (parentId == null) {
          return;
        }

        const parent = await this.getById(parentId);
        if (parent != null) {
          await checkForParent(parent);
        }
      }
    };

    await checkForParent(newParent);

    const iv = await randomBytes(16);
    const [authTag, encryptedKey] = encryptSymmetric(
      newParent[FileManager.KEY_UNLOCKED_AES_KEY],
      iv,
      file[FileManager.KEY_UNLOCKED_AES_KEY]
    );

    await this.update(file, {
      [FileManager.KEY_PARENT_FILE_ID]: newParent.id,
      [FileManager.KEY_ENCRYPTED_AES_KEY]: encryptedKey,
      [FileManager.KEY_ENCRYPTED_AES_KEY_IV]: iv,
      [FileManager.KEY_ENCRYPTED_AES_KEY_AUTH_TAG]: authTag
    });
    await this.update(newParent, {});
  }

  public async trash(file: UnlockedFile): Promise<void> {
    await this.update(file, {
      [FileManager.KEY_DELETED]: true
    });
  }
}

Database.register(FileManager);
