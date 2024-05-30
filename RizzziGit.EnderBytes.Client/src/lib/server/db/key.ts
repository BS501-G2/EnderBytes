import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { generateKeyPair } from '../utils';
import { UserKeyManager, type UnlockedUserKey } from './user-key';
import { KeyShardManager } from './key-shard';

export interface Key extends Data<KeyManager, Key> {
  [KeyManager.KEY_PUBLIC_KEY]: Uint8Array;
}

export interface UnlockedKey extends Key {
  privateKey: Uint8Array;
}

export class KeyManager extends DataManager<KeyManager, Key> {
  public static readonly NAME = 'Key';
  public static readonly VERSION = 1;

  public static readonly KEY_PUBLIC_KEY = 'publicKey';

  public constructor(db: Database, transaction: () => Knex.Transaction<Key>) {
    super(db, transaction, KeyManager.NAME, KeyManager.VERSION);
  }

  protected upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): void {
    if (oldVersion < 1) {
      table.binary(KeyManager.KEY_PUBLIC_KEY).notNullable();
    }
  }

  public async create(userKey: UnlockedUserKey): Promise<UnlockedKey> {
    const [privateKey, publicKey] = await generateKeyPair();
    const key = await this.insert({
      publicKey
    });
    const unlockedKey: UnlockedKey = {
      ...key,
      privateKey
    };

    const keyShardManager = this.getManager(KeyShardManager);
    await keyShardManager.create(unlockedKey, userKey);

    return unlockedKey;
  }

  public async delete(key: UnlockedKey): Promise<void> {
    const keyShardManager = this.getManager(KeyShardManager);

    await keyShardManager.deleteWhere([
      [KeyShardManager.KEY_KEY_ID, '=', key[KeyManager.KEY_DATA_ID]]
    ]);

    await super.delete(key);
  }
}

Database.register(KeyManager);
