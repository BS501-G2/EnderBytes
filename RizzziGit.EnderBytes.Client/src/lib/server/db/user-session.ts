import type { Knex } from 'knex';
import { DataManager, Database, type Data, type OrderByClause, type QueryOptions } from '../db';
import type { User } from './user';
import { type UnlockedUserKey, type UserKey } from './user-key';
import { decryptSymmetric, encryptSymmetric, randomBytes } from '../utils';
import { KeyShardManager } from './key-shard';

export interface UserSession extends Data<UserSessionManager, UserSession> {
  expireTime: number;
  userId: number;
  originKeyId: number;

  iv: Uint8Array;

  encryptedAuthTag: Uint8Array;
  encryptedPrivateKey: Uint8Array;
}

export interface UnlockedUserSession extends UserSession {
  privateKey: Uint8Array;
  key: Uint8Array;
}

export class UserSessionManager extends DataManager<UserSessionManager, UserSession> {
  public static readonly NAME = 'UserSession';
  public static readonly VERSION = 1;

  public static readonly KEY_EXPIRE_TIME = 'expireTime';
  public static readonly KEY_USER_ID = 'userId';
  public static readonly KEY_ORIGIN_KEY_ID = 'originKeyId';
  public static readonly KEY_IV = 'iv';

  public static readonly KEY_ENCRYPTED_AUTH_TAG = 'encryptedAuthTag';
  public static readonly KEY_ENCRYPTED_PRIVATE_KEY = 'encryptedPrivateKey';

  public constructor(db: Database, transaction: () => Knex.Transaction<UserSession>) {
    super(db, transaction, UserSessionManager.NAME, UserSessionManager.VERSION);
  }

  protected async upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): Promise<void> {
    if (oldVersion < 1) {
      table.integer(UserSessionManager.KEY_EXPIRE_TIME).notNullable();
      table.integer(UserSessionManager.KEY_USER_ID).notNullable();
      table.integer(UserSessionManager.KEY_ORIGIN_KEY_ID).notNullable();
      table.binary(UserSessionManager.KEY_IV).notNullable();
      table.binary(UserSessionManager.KEY_ENCRYPTED_AUTH_TAG).notNullable();
      table.binary(UserSessionManager.KEY_ENCRYPTED_PRIVATE_KEY).notNullable();
    }
  }

  public async create(
    user: User,
    userKey: UnlockedUserKey,
    expireTime: number
  ): Promise<UnlockedUserSession> {
    const iv = await randomBytes(16);
    const key = await randomBytes(32);
    const [authTag, encryptedPrivateKey] = encryptSymmetric(key, iv, userKey.privateKey);

    const unlockedUserSession: UnlockedUserSession = {
      ...(await this.insert({
        expireTime,
        userId: user.id,
        originKeyId: userKey.id,
        iv,
        encryptedAuthTag: authTag,
        encryptedPrivateKey
      })),
      key,
      privateKey: userKey.privateKey
    };

    const toDelete: UserSession[] = [];
    for (const userSession of (
      await this.query({
        where: [[UserSessionManager.KEY_USER_ID, '=', user.id]],
        orderBy: [[DataManager.KEY_DATA_ID, true]]
      })
    ).slice(10)) {
      toDelete.push(userSession);
    }

    await Promise.all(toDelete.map((userSession) => this.delete(userSession)));

    return unlockedUserSession;
  }

  public async unlock(userSession: UserSession, key: Uint8Array): Promise<UnlockedUserSession> {
    const privateKey = decryptSymmetric(
      key,
      userSession.iv,
      userSession.encryptedPrivateKey,
      userSession.encryptedAuthTag
    );
    return {
      ...userSession,
      key,
      privateKey
    };
  }

  public unlockUserKey(userSession: UnlockedUserSession, userKey: UserKey): UnlockedUserKey {
    return {
      ...userKey,
      privateKey: userSession.privateKey
    };
  }
}

Database.register(UserSessionManager);
