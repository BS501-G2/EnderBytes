import knex, { type Knex } from 'knex';
import { existsSync as fileExists, mkdirSync as createFolder } from 'fs';

import { createTaskQueue, type TaskQueue } from '../task-queue';

const DATABASE_TRANSACTION_QUEUE = Symbol('DatabaseTransactionQueue');
const DATABASE_CURRENT_TRANSACTION = Symbol('DatabaseCurrentTransaction');
const DATABASE_CONNECTION = Symbol('DatabaseConnection');

export type DataManagerConstructor<M extends DataManager<M, D>, D extends Data<M, D>> = new (
  db: Database,
  transaction: () => Knex.Transaction<D, D[]>
) => M;

export type DataManagerRegistration<M extends DataManager<M, D>, D extends Data<M, D>> = {
  init: DataManagerConstructor<M, D>;
};

export type DataManagerInstance<M extends DataManager<M, D>, D extends Data<M, D>> = {
  init: DataManagerConstructor<M, D>;
  instance: M;
};

interface VersionTable {
  name: string;
  version: number;
}

const DATABASE_INSTANTIATING = Symbol('DatabaseInstantiating');
const DATABASE_INSTANCE = Symbol('DatabaseInstance');
const DATABASE_INIT = Symbol('DatabaseInit');
const DATABASE_GET_VERSION = Symbol('GetVersion');

export class Database {
  public static readonly managers: DataManagerRegistration<never, never>[] = [];
  public static [DATABASE_INSTANTIATING]: boolean = false;
  public static [DATABASE_INSTANCE]: Database | null = null;

  public static register<M extends DataManager<M, D>, D extends Data<M, D>>(
    init: DataManagerConstructor<M, D>
  ) {
    this.managers.push({ init } as unknown as DataManagerRegistration<never, never>);
  }

  public static async getInstance(): Promise<Database> {
    if (this[DATABASE_INSTANCE] != null) {
      return this[DATABASE_INSTANCE];
    }

    try {
      this[DATABASE_INSTANTIATING] = true;

      try {
        const instance = (this[DATABASE_INSTANCE] = new this());

        await instance[DATABASE_INIT]();
        return instance;
      } catch (error: any) {
        throw error;
      }
    } finally {
      this[DATABASE_INSTANTIATING] = false;
    }
  }

  public constructor() {
    if (!Database[DATABASE_INSTANTIATING]) {
      throw new Error('Invalid call to Database constructor');
    }

    if (!fileExists('./.db')) {
      createFolder('./.db');
    }

    this[DATABASE_CONNECTION] = knex({
      client: 'sqlite3',
      connection: {
        filename: './.db/database.sqlite'
      },
      useNullAsDefault: true
    });
    this[DATABASE_CURRENT_TRANSACTION] = null;
    this[DATABASE_TRANSACTION_QUEUE] = createTaskQueue();
  }

  private readonly [DATABASE_CONNECTION]: Knex;
  private readonly [DATABASE_TRANSACTION_QUEUE]: TaskQueue;
  private [DATABASE_CURRENT_TRANSACTION]: Knex.Transaction | null;

  public readonly managers: DataManagerInstance<never, never>[] = [];

  public get db(): Knex.Transaction {
    const transaction = this[DATABASE_CURRENT_TRANSACTION];

    if (transaction == null) {
      throw new Error('Transaction is null');
    }

    return transaction;
  }

  private async [DATABASE_INIT](): Promise<void> {
    await this[DATABASE_CONNECTION].raw('PRAGMA synchtonization = ON;');
    await this[DATABASE_CONNECTION].raw('PRAGMA journal_mode = MEMORY;');

    await this[DATABASE_CONNECTION].raw(
      `create table if not exists version (name text primary key, version integer);`
    );

    for (const entry of Database.managers) {
      const instance = new entry.init(this, () => <any>this.db);

      this.managers.push({ init: entry.init, instance: instance as never });
    }

    await this.transact(async () => {
      for (const entry of this.managers) {
        const instance = entry.instance as DataManager<never, never>;
        const version = (await this[DATABASE_GET_VERSION](instance.name)) ?? undefined;

        await instance.init(version ?? undefined);
      }
    });
  }

  private async [DATABASE_GET_VERSION](name: string): Promise<number | null> {
    const version = await this.db
      .select<VersionTable>('*')
      .from('version')
      .where('name', '=', name)
      .first();

    return version?.version ?? null;
  }

  public async transact<T, A extends any[] = never[]>(
    callback: (db: Knex.Transaction, ...args: A) => T | Promise<T>,
    ...args: A
  ): Promise<T> {
    const transaction = await this[DATABASE_TRANSACTION_QUEUE].pushQueue<T, A>(
      async (...args: A) => {
        if (this[DATABASE_CURRENT_TRANSACTION]) {
          throw new Error('Transaction has already been started');
        }

        return await this[DATABASE_CONNECTION].transaction(async (transaction) => {
          this[DATABASE_CURRENT_TRANSACTION] = transaction;
          try {
            return await callback(transaction, ...args);
          } finally {
            this[DATABASE_CURRENT_TRANSACTION] = null;
          }
        });
      },
      ...args
    );
    this[DATABASE_CURRENT_TRANSACTION] = null;
    return transaction;
  }

  public getManager<M extends DataManager<M, D>, D extends Data<M, D>>(
    init: DataManagerConstructor<M, D>
  ): M {
    return this.managers.find((entry) => entry.init === (init as any))!.instance as M;
  }
}

export interface Data<M extends DataManager<M, D>, D extends Data<M, D>> {
  id: number;
  createTime: number;
  updateTime: number;
}

export interface IDataManager {}

const SYMBOL_DATA_MANAGER_DATABASE = Symbol('Database');
const SYMBOL_DATA_MANAGER_TRANSACTION = Symbol('DatabaseTransaction');

export abstract class DataManager<M extends DataManager<M, D>, D extends Data<M, D>>
  implements IDataManager
{
  public static readonly KEY_ID = 'id';
  public static readonly KEY_CREATE_TIME = 'createTime';
  public static readonly KEY_UPDATE_TIME = 'updateTime';

  public constructor(
    db: Database,
    transaction: () => Knex.Transaction<D>,
    name: string,
    version: number
  ) {
    this[SYMBOL_DATA_MANAGER_DATABASE] = db;
    this[SYMBOL_DATA_MANAGER_TRANSACTION] = transaction;
    this.name = name;
    this.version = version;
  }

  private readonly [SYMBOL_DATA_MANAGER_DATABASE]: Database;
  private readonly [SYMBOL_DATA_MANAGER_TRANSACTION]: () => Knex.Transaction<D>;

  public readonly name: string;
  public readonly version: number;

  public getManager<M extends DataManager<M, D>, D extends Data<M, D>>(
    init: DataManagerConstructor<M, D>
  ): M {
    return this[SYMBOL_DATA_MANAGER_DATABASE].getManager(init as any) as M;
  }

  public get db(): Knex.Transaction<D, D[]> {
    return this[SYMBOL_DATA_MANAGER_TRANSACTION]();
  }

  protected abstract upgrade(oldVersion?: number): Promise<void>;
  protected new(obj: Omit<D, keyof Data<never, never>>): D {
    const createTime = Date.now();
    return <never>{
      createTime,
      updateTime: createTime,
      ...obj
    };
  }

  public async init(version?: number): Promise<void> {
    const exists = await this.db.raw(
      `select name from sqlite_master where type='table' and name='${this.name}';`
    );

    if (exists.length == 0) {
      await this.db.schema.createTable(this.name, (table) => {
        table.increments(DataManager.KEY_ID);
        table.integer(DataManager.KEY_CREATE_TIME).notNullable();
        table.integer(DataManager.KEY_UPDATE_TIME).notNullable();
      });
    }

    if (version == null || version < this.version) {
      await this.upgrade(version);

      const result = await this.db
        .table<VersionTable>('version')
        .update({ version: this.version })
        .into('version')
        .where('name', '=', this.name);
      if (result == 0) {
        await this.db
          .table<VersionTable>('version')
          .insert({ name: this.name, version: this.version });
      }
    }
  }

  public async get(id: number): Promise<D | null> {
    const result = await this.db
      .select('*')
      .from(this.name)
      .where(DataManager.KEY_ID, '=', id)
      .first();

    return result ?? null;
  }

  public async delete(user: D): Promise<void> {
    await this.db.delete().from(this.name).where(DataManager.KEY_ID, '=', user.id);
  }
}
