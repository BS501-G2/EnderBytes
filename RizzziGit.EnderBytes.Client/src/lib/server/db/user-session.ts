import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import type { User } from './user';
import {
  decryptSymmetric,
  encryptSymmetric,
  hashPayload,
  randomBytes,
  type UnlockedUserKey,
  type UserKey
} from './user-key';

export interface UserSession extends Data<UserSessionManager, UserSession> {
  expireTime: number;
  userId: number;
  originKeyId: number;

  iv: Buffer;

  encryptedAuthTag: Buffer;
  encryptedPrivateKey: Buffer;
}

export interface UnlockedUserSession extends UserSession {
  privateKey: Buffer;
  key: Buffer;
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

  public async list(user: User | null, originkey: UserKey | null): Promise<UserSession[]> {
    let a = this.db.select('*').from<UserSession>(this.name);

    if (user != null) {
      a = a.where(UserSessionManager.KEY_USER_ID, '=', user.id);
    }

    if (originkey != null) {
      a = a.where(UserSessionManager.KEY_ORIGIN_KEY_ID, '=', originkey.id);
    }

    return await a;
  }

  public async delete(userSession: UserSession): Promise<void> {
    await this.db.delete().from(this.name).where(DataManager.KEY_DATA_ID, '=', userSession.id);
  }

  public async deleteAllFromUser(user: User): Promise<void> {
    await this.db.delete().from(this.name).where(UserSessionManager.KEY_USER_ID, '=', user.id);
  }

  public async create(
    user: User,
    originKey: UnlockedUserKey,
    expireTime: number
  ): Promise<UnlockedUserSession> {
    const iv = await randomBytes(16);
    const key = await randomBytes(32);
    const [authTag, encryptedPrivateKey] = encryptSymmetric(key, iv, originKey.privateKey);

    return {
      ...await this.insert({
        expireTime,
        userId: user.id,
        originKeyId: originKey.id,
        iv,
        encryptedAuthTag: authTag,
        encryptedPrivateKey
      }),
      key,
      privateKey: originKey.privateKey
    };
  }

  public async unlock(userSession: UnlockedUserSession, key: Buffer): Promise<UnlockedUserSession> {
    const privateKey = decryptSymmetric(
      userSession.key,
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
}

Database.register(UserSessionManager);
