import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { type User } from './user';
import type { UserKeyType } from '$lib/shared/db';
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

  [UserKeyManager.KEY_ENCRYPTED_PRIVATE_KEY]: Uint8Array;
  [UserKeyManager.KEY_ENCRYPTED_AUTH_TAG]: Uint8Array;
  [UserKeyManager.KEY_PUBLIC_KEY]: Uint8Array;

  [UserKeyManager.KEY_TEST_BYTES]: Uint8Array;
  [UserKeyManager.KEY_TEST_ENCRYPTED_BYTES]: Uint8Array;
}

export interface UnlockedUserKey extends UserKey {
  privateKey: Uint8Array;
}

export class UserKeyManager extends DataManager<UserKeyManager, UserKey> {
  public static readonly KEY_USER_ID = 'userId';
  public static readonly KEY_TYPE = 'type';

  public static readonly KEY_ITERATIONS = 'iterations';
  public static readonly KEY_SALT = 'salt';
  public static readonly KEY_IV = 'iv';

  public static readonly KEY_ENCRYPTED_PRIVATE_KEY = 'encryptedPrivateKey';
  public static readonly KEY_ENCRYPTED_AUTH_TAG = 'encryptedAuthTag';
  public static readonly KEY_PUBLIC_KEY = 'publicKey';

  public static readonly KEY_TEST_BYTES = 'testBytes';
  public static readonly KEY_TEST_ENCRYPTED_BYTES = 'testEncryptedBytes';

  public constructor(db: Database, transaction: () => Knex.Transaction<UserKey>) {
    super(db, transaction, 'UserKey', 1);
  }

  protected async upgrade(table: Knex.TableBuilder, oldVersion: number = 0): Promise<void> {
    if (oldVersion < 1) {
      table.integer(UserKeyManager.KEY_USER_ID).notNullable();
      table.integer(UserKeyManager.KEY_TYPE).notNullable();

      table.integer(UserKeyManager.KEY_ITERATIONS).notNullable();
      table.string(UserKeyManager.KEY_SALT).notNullable();
      table.string(UserKeyManager.KEY_IV).notNullable();

      table.binary(UserKeyManager.KEY_ENCRYPTED_PRIVATE_KEY).notNullable();
      table.binary(UserKeyManager.KEY_ENCRYPTED_AUTH_TAG).notNullable();
      table.binary(UserKeyManager.KEY_PUBLIC_KEY).notNullable();

      table.binary(UserKeyManager.KEY_TEST_BYTES).notNullable();
      table.binary(UserKeyManager.KEY_TEST_ENCRYPTED_BYTES).notNullable();
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
    const [key, testBytes] = await Promise.all([hashPayload(payload, salt), randomBytes(32)]);
    const [[encryptedAuthTag, encryptedPrivateKey], testEncryptedBytes] = await Promise.all([
      encryptSymmetric(key, iv, privateKey),
      encryptAsymmetric(publicKey, testBytes)
    ]);

    const userKey = await this.insert({
      userId: user.id,
      type,

      iterations: 10000,
      salt: salt,
      iv: iv,

      encryptedPrivateKey,
      encryptedAuthTag,
      publicKey,

      testBytes: testBytes,
      testEncryptedBytes: testEncryptedBytes
    });

    return this.unlock(userKey, payload);
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
      key.iv,
      key.encryptedPrivateKey,
      key.encryptedAuthTag
    );

    return {
      ...key,

      privateKey
    };
  }

  public decrypt(key: UnlockedUserKey, payload: Uint8Array): Uint8Array {
    return decryptAsymmetric(key.privateKey, payload);
  }

  public encrypt(key: UserKey, payload: Uint8Array): Uint8Array {
    return encryptAsymmetric(key.publicKey, payload);
  }

  public async delete(key: UserKey): Promise<void> {
    await super.delete(key);
  }
}

Database.register(UserKeyManager);
