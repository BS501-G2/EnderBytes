import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { KeyManager, type Key, type UnlockedKey } from './key';
import type { User } from './user';
import { UserKeyManager, type UnlockedUserKey, type UserKey } from './user-key';
import { encryptAsymmetric } from '../utils';

export interface KeyShard extends Data<KeyShardManager, KeyShard> {
  [KeyShardManager.KEY_USER_ID]: number;
  [KeyShardManager.KEY_KEY_ID]: number;
  [KeyShardManager.KEY_ENCRYPTED_PRIVATE_KEY]: Uint8Array;
}

export interface UnlockedKeyShard extends KeyShard {
  privateKey: Uint8Array;
}

export class KeyShardManager extends DataManager<KeyShardManager, KeyShard> {
  public static readonly NAME = 'KeyShard';
  public static readonly VERSION = 1;

  public static readonly KEY_USER_ID = 'userId';
  public static readonly KEY_KEY_ID = 'keyId';
  public static readonly KEY_ENCRYPTED_PRIVATE_KEY = 'encryptedPrivateKey';

  public constructor(db: Database, transaction: () => Knex.Transaction<KeyShard>) {
    super(db, transaction, KeyShardManager.NAME, KeyShardManager.VERSION);
  }

  protected upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): void {
    if (oldVersion < 1) {
      table.string(KeyShardManager.KEY_USER_ID).notNullable();
      table.string(KeyShardManager.KEY_KEY_ID).notNullable();
      table.binary(KeyShardManager.KEY_ENCRYPTED_PRIVATE_KEY).notNullable();
    }
  }

  public async create(key: UnlockedKey, userKey: UnlockedUserKey): Promise<UnlockedKeyShard> {
    const userKeyManager = this.getManager(UserKeyManager);
    const encryptedPrivateKey = userKeyManager.encrypt(userKey, key.privateKey);

    const keyShard = await this.insert({
      userId: userKey[UserKeyManager.KEY_USER_ID],
      keyId: key[KeyManager.KEY_DATA_ID],
      encryptedPrivateKey
    });

    return {
      ...keyShard,
      privateKey: key.privateKey
    };
  }
}

Database.register(KeyShardManager);
