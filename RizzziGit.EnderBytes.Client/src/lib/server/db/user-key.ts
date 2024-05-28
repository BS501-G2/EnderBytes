import * as Crypto from 'crypto';

import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { UserManager, type User } from './user';

export type KeyPair = [privateKey: Buffer, publicKey: Buffer];

export const generateKeyPair = (): Promise<KeyPair> =>
  new Promise<KeyPair>((resolve, reject) => {
    Crypto.generateKeyPair(
      'rsa',
      {
        modulusLength: 2048,
        publicKeyEncoding: {
          type: 'spki',
          format: 'pem'
        },
        privateKeyEncoding: {
          type: 'pkcs8',
          format: 'pem'
        }
      },
      function (error: Error | null, publicKey: string, privateKey: string) {
        if (error) {
          reject(error);
        } else {
          resolve([Buffer.from(privateKey, 'utf-8'), Buffer.from(publicKey, 'utf-8')]);
        }
      }
    );
  });

export const hashPayload = (payload: Buffer, salt: Buffer): Promise<Buffer> =>
  new Promise((resolve, reject) => {
    Crypto.scrypt(payload, salt, 32, (error: Error | null, hash: Buffer) => {
      if (error) {
        reject(error);
      } else {
        resolve(hash);
      }
    });
  });

export const randomBytes = (length: number): Promise<Buffer> =>
  new Promise((resolve, reject) => {
    Crypto.randomBytes(length, (error: Error | null, buffer: Buffer) => {
      if (error) {
        reject(error);
      } else {
        resolve(buffer);
      }
    });
  });

export const encryptSymmetric = (
  key: Buffer,
  iv: Buffer,
  buffer: Buffer
): [authTag: Buffer, output: Buffer] => {
  const cipher = Crypto.createCipheriv('aes-256-gcm', key, iv);
  const output = Buffer.concat([cipher.update(buffer), cipher.final()]);
  const authTag = cipher.getAuthTag();

  return [authTag, output];
};

export const decryptSymmetric = (
  key: Buffer,
  iv: Buffer,
  buffer: Buffer,
  authTag: Buffer
): Buffer => {
  const decipher = Crypto.createDecipheriv('aes-256-gcm', key, iv);
  decipher.setAuthTag(authTag);

  return Buffer.concat([decipher.update(buffer), decipher.final()]);
};

export const decryptAsymmetric = (privateKey: Buffer, buffer: Buffer): Buffer =>
  Crypto.privateDecrypt(privateKey, buffer);

export const encryptAsymmetric = (publicKey: Buffer, buffer: Buffer): Buffer =>
  Crypto.publicEncrypt(publicKey, buffer);

export enum UserKeyType {
  Password = 0,
  Session
}

export interface UserKey extends Data<UserKeyManager, UserKey> {
  [UserKeyManager.KEY_USER_ID]: number;
  [UserKeyManager.KEY_TYPE]: UserKeyType;

  [UserKeyManager.KEY_ITERATIONS]: number;
  [UserKeyManager.KEY_SALT]: Buffer;
  [UserKeyManager.KEY_IV]: Buffer;

  [UserKeyManager.KEY_ENCRYPTED_PRIVATE_KEY]: Buffer;
  [UserKeyManager.KEY_ENCRYPTED_AUTH_TAG]: Buffer;
  [UserKeyManager.KEY_PUBLIC_KEY]: Buffer;

  [UserKeyManager.KEY_TEST_BYTES]: Buffer;
  [UserKeyManager.KEY_TEST_ENCRYPTED_BYTES]: Buffer;
}

export interface UnlockedUserKey extends UserKey {
  privateKey: Buffer;
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
    privateKey: Buffer,
    publicKey: Buffer,
    type: UserKeyType,
    payload: Buffer
  ): Promise<UnlockedUserKey> {
    const [salt, iv] = await Promise.all([randomBytes(32), randomBytes(16)]);
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

  public async list(filterType?: UserKeyType): Promise<UserKey[]> {
    let a = this.db.select('*').from<UserKey>(this.name).orderBy(DataManager.KEY_DATA_ID, 'asc');

    if (filterType != null) {
      a = a.where(UserKeyManager.KEY_TYPE, '=', filterType);
    }

    return await a;
  }

  public async unlock(key: UserKey, payload: Buffer): Promise<UnlockedUserKey> {
    const privateKey = await decryptSymmetric(
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

  public async decrypt(key: UnlockedUserKey, payload: Buffer): Promise<Buffer> {
    return decryptAsymmetric(key.privateKey, payload);
  }

  public async encrypt(key: UserKey, payload: Buffer): Promise<Buffer> {
    return encryptAsymmetric(key.publicKey, payload);
  }

  public async delete(key: UserKey): Promise<void> {
    await super.delete(key);
  }

  public async deleteAllFromUser(user: User): Promise<void> {
    await this.deleteWhere([[UserKeyManager.KEY_USER_ID, '=', user.id]]);
  }
}

Database.register(UserKeyManager);
