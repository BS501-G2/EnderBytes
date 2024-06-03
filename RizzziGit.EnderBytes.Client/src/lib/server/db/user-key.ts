import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { type User } from './user';
import { UserKeyType } from '$lib/shared/db';
import {
  decryptAsymmetric,
  decryptSymmetric,
  encryptAsymmetric,
  encryptSymmetric,
  generateKeyPair,
  hashPayload,
  randomBytes
} from '../utils';

export interface UserKey extends Data<UserKeyManager, UserKey> {
  [UserKeyManager.KEY_USER_ID]: number;
  [UserKeyManager.KEY_TYPE]: UserKeyType;
  [UserKeyManager.KEY_ITERATIONS]: number;
  [UserKeyManager.KEY_SALT]: Uint8Array;
  [UserKeyManager.KEY_IV]: Uint8Array;
  [UserKeyManager.KEY_AUTH_TAG]: Uint8Array;

  [UserKeyManager.KEY_ENCRYPTED_PRIVATE_KEY]: Uint8Array;
  [UserKeyManager.KEY_PUBLIC_KEY]: Uint8Array;
}

export interface UnlockedUserKey extends UserKey {
  [UserKeyManager.KEY_UNLOCKED_PRIVATE_KEY]: Uint8Array;
}

export class UserKeyManager extends DataManager<UserKeyManager, UserKey> {
  public static readonly KEY_USER_ID = 'userId';
  public static readonly KEY_TYPE = 'type';

  public static readonly KEY_ITERATIONS = 'iterations';
  public static readonly KEY_SALT = 'salt';
  public static readonly KEY_IV = 'iv';
  public static readonly KEY_AUTH_TAG = 'authTag';

  public static readonly KEY_ENCRYPTED_PRIVATE_KEY = 'encryptedPrivateKey';
  public static readonly KEY_PUBLIC_KEY = 'publicKey';

  public static readonly KEY_UNLOCKED_PRIVATE_KEY = 'privateKey';

  public constructor(db: Database, transaction: () => Knex.Transaction<UserKey>) {
    super(db, transaction, 'UserKey', 1);
  }

  protected get ftsColumns(): (keyof UserKey)[] {
    return [];
  }

  protected async upgrade(table: Knex.TableBuilder, oldVersion: number = 0): Promise<void> {
    if (oldVersion < 1) {
      table.integer(UserKeyManager.KEY_USER_ID).notNullable();
      table.integer(UserKeyManager.KEY_TYPE).notNullable();

      table.integer(UserKeyManager.KEY_ITERATIONS).notNullable();
      table.string(UserKeyManager.KEY_SALT).notNullable();
      table.string(UserKeyManager.KEY_IV).notNullable();

      table.binary(UserKeyManager.KEY_ENCRYPTED_PRIVATE_KEY).notNullable();
      table.binary(UserKeyManager.KEY_AUTH_TAG).notNullable();
      table.binary(UserKeyManager.KEY_PUBLIC_KEY).notNullable();
    }
  }

  public async create(
    user: User,
    type: UserKeyType,
    payload: Uint8Array
  ): Promise<UnlockedUserKey> {
    const [[privateKey, publicKey], salt, iv] = await Promise.all([
      generateKeyPair(),
      randomBytes(32),
      randomBytes(16)
    ]);
    const key = await hashPayload(payload, salt);
    const [encryptedAuthTag, encryptedPrivateKey] = await encryptSymmetric(key, iv, privateKey);

    const userKey = await this.insert({
      [UserKeyManager.KEY_USER_ID]: user.id,
      [UserKeyManager.KEY_TYPE]: type,

      [UserKeyManager.KEY_ITERATIONS]: 10000,
      [UserKeyManager.KEY_SALT]: salt,
      [UserKeyManager.KEY_IV]: iv,

      [UserKeyManager.KEY_ENCRYPTED_PRIVATE_KEY]: encryptedPrivateKey,
      [UserKeyManager.KEY_AUTH_TAG]: encryptedAuthTag,
      [UserKeyManager.KEY_PUBLIC_KEY]: publicKey
    });

    const unlocked = this.unlock(userKey, payload);
    return unlocked;
  }

  public async list(user: User, filterType?: UserKeyType): Promise<UserKey[]> {
    return await this.query({
      where: [
        filterType != null ? [UserKeyManager.KEY_TYPE, '=', filterType] : null,
        [UserKeyManager.KEY_USER_ID, '=', user.id]
      ]
    });
  }

  public async findByPayload(
    user: User,
    type: UserKeyType,
    payload: Uint8Array
  ): Promise<UnlockedUserKey | null> {
    const keys = await this.list(user, type);

    for (const key of keys) {
      try {
        const unlocked = await this.unlock(key, payload);

        if (unlocked != null) {
          return unlocked;
        }
      } catch (error: any) {
        continue;
      }
    }

    return null;
  }

  public async unlock(key: UserKey, payload: Uint8Array): Promise<UnlockedUserKey> {
    const privateKey = decryptSymmetric(
      await hashPayload(payload, key.salt),

      key[UserKeyManager.KEY_IV],
      key[UserKeyManager.KEY_ENCRYPTED_PRIVATE_KEY],
      key[UserKeyManager.KEY_AUTH_TAG]
    );

    return {
      ...key,

      [UserKeyManager.KEY_UNLOCKED_PRIVATE_KEY]: privateKey
    };
  }

  public async setPassword(
    user: User,
    oldPassword: string,
    newPassword: string
  ): Promise<UnlockedUserKey> {
    const [userKey] = await this.list(user, UserKeyType.Password);
    if (userKey == null) {
      throw new Error('User has no password');
    }

    let unlockedUserKey: UnlockedUserKey;
    try {
      unlockedUserKey = await this.unlock(userKey, new TextEncoder().encode(oldPassword));
    } catch {
      throw new Error('Wrong password');
    }

    const newKey = await hashPayload(new TextEncoder().encode(newPassword), unlockedUserKey.salt);
    const [encryptedAuthTag, encryptedPrivateKey] = encryptSymmetric(
      newKey,
      unlockedUserKey[UserKeyManager.KEY_IV],
      unlockedUserKey[UserKeyManager.KEY_UNLOCKED_PRIVATE_KEY]
    );

    const newUserKey = await this.update(userKey, {
      [UserKeyManager.KEY_ENCRYPTED_PRIVATE_KEY]: encryptedPrivateKey,
      [UserKeyManager.KEY_AUTH_TAG]: encryptedAuthTag
    });

    return this.unlock(newUserKey, new TextEncoder().encode(newPassword));
  }

  public decrypt(key: UnlockedUserKey, payload: Uint8Array): Uint8Array {
    return decryptAsymmetric(key.privateKey, payload);
  }

  public encrypt(key: UserKey, payload: Uint8Array): Uint8Array {
    return encryptAsymmetric(key.publicKey, payload);
  }
}

Database.register(UserKeyManager);
