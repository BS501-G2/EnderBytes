import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { UserKeyManager, type UnlockedUserKey, type UserKey } from './user-key';
import { decryptSymmetric, encryptSymmetric, randomBytes } from '../utils';
import { userSessionExpiryDuration } from '$lib/shared/api';

export interface UserSession extends Data<UserSessionManager, UserSession> {
  [UserSessionManager.KEY_EXPIRE_TIME]: number;
  [UserSessionManager.KEY_USER_ID]: number;
  [UserSessionManager.KEY_ORIGIN_KEY_ID]: number;

  [UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY]: Uint8Array;
  [UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY_IV]: Uint8Array;
  [UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY_AUTH_TAG]: Uint8Array;
}

export interface UnlockedUserSession extends UserSession {
  [UserSessionManager.KEY_UNLOCKED_KEY]: Uint8Array;
  [UserSessionManager.KEY_UNLOCKED_PRIVATE_KEY]: Uint8Array;
}

export class UserSessionManager extends DataManager<UserSessionManager, UserSession> {
  public static readonly NAME = 'UserSession';
  public static readonly VERSION = 1;

  public static readonly KEY_EXPIRE_TIME = 'expireTime';
  public static readonly KEY_USER_ID = 'userId';
  public static readonly KEY_ORIGIN_KEY_ID = 'originKeyId';

  public static readonly KEY_ENCRYPTED_PRIVATE_KEY = 'encryptedPrivateKey';
  public static readonly KEY_ENCRYPTED_PRIVATE_KEY_IV = 'iv';
  public static readonly KEY_ENCRYPTED_PRIVATE_KEY_AUTH_TAG = 'authTag';

  public static readonly KEY_UNLOCKED_KEY = 'key';
  public static readonly KEY_UNLOCKED_PRIVATE_KEY = 'privateKey';

  public constructor(db: Database, transaction: () => Knex.Transaction<UserSession>) {
    super(db, transaction, UserSessionManager.NAME, UserSessionManager.VERSION);
  }

  protected get ftsColumns(): (keyof UserSession)[] {
    return [];
  }

  protected async upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): Promise<void> {
    if (oldVersion < 1) {
      table.integer(UserSessionManager.KEY_EXPIRE_TIME).notNullable();
      table.integer(UserSessionManager.KEY_USER_ID).notNullable();
      table.integer(UserSessionManager.KEY_ORIGIN_KEY_ID).notNullable();

      table.binary(UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY).notNullable();
      table.binary(UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY_IV).notNullable();
      table.binary(UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY_AUTH_TAG).notNullable();
    }
  }

  public async create(
    unlockedUserKey: UnlockedUserKey,
    expireDuration: number = userSessionExpiryDuration
  ): Promise<UnlockedUserSession> {
    const [key, iv] = await Promise.all([
      randomBytes(32),
      randomBytes(16)
    ]);

    const [authTag, encryptedPrivateKey] = encryptSymmetric(
      key,
      iv,
      unlockedUserKey[UserKeyManager.KEY_UNLOCKED_PRIVATE_KEY]
    );

    const userSession = await this.insert({
      [UserSessionManager.KEY_EXPIRE_TIME]: Date.now() + expireDuration,
      [UserSessionManager.KEY_USER_ID]: unlockedUserKey[UserKeyManager.KEY_USER_ID],
      [UserSessionManager.KEY_ORIGIN_KEY_ID]: unlockedUserKey[UserKeyManager.KEY_DATA_ID],

      [UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY]: encryptedPrivateKey,
      [UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY_IV]: iv,
      [UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY_AUTH_TAG]: authTag
    });

    return {
      ...userSession,

      [UserSessionManager.KEY_UNLOCKED_KEY]: key,
      [UserSessionManager.KEY_UNLOCKED_PRIVATE_KEY]: unlockedUserKey.privateKey
    };
  }

  public unlock(userSession: UserSession, key: Uint8Array): UnlockedUserSession {
    return {
      ...userSession,

      [UserSessionManager.KEY_UNLOCKED_KEY]: key,
      [UserSessionManager.KEY_UNLOCKED_PRIVATE_KEY]: decryptSymmetric(
        key,
        userSession[UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY_IV],
        userSession[UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY],
        userSession[UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY_AUTH_TAG]
      )
    };
  }

  public unlockKey(userSession: UnlockedUserSession, userKey: UserKey): UnlockedUserKey {
    return {
      ...userKey,

      [UserKeyManager.KEY_UNLOCKED_PRIVATE_KEY]:
        userSession[UserSessionManager.KEY_UNLOCKED_PRIVATE_KEY]
    };
  }
}

Database.register(UserSessionManager);
