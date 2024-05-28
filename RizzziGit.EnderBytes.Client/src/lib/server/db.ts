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

interface DataHolder {
  [DataManager.KEY_HOLDER_ID]: number;
  [DataManager.KEY_HOLDER_DELETED]: boolean;
}

export interface Data<M extends DataManager<M, D>, D extends Data<M, D>> {
  [DataManager.KEY_DATA_VERSION_ID]: number;
  [DataManager.KEY_DATA_ID]: number;
  [DataManager.KEY_DATA_CREATE_TIME]: number;
  [DataManager.KEY_DATA_PREVIOUS_ID]: number | null;
  [DataManager.KEY_DATA_NEXT_ID]: number | null;
}

export interface IDataManager {}

const SYMBOL_DATA_MANAGER_DATABASE = Symbol('Database');
const SYMBOL_DATA_MANAGER_TRANSACTION = Symbol('Transaction');
const SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME = Symbol('HolderTableName');
const SYMBOL_DATA_MANAGER_DATA_TABLE_NAME = Symbol('DataTableName');

export abstract class DataManager<M extends DataManager<M, D>, D extends Data<M, D>>
  implements IDataManager
{
  public static readonly KEY_HOLDER_ID = 'id';
  public static readonly KEY_HOLDER_DELETED = 'deleted';

  public static readonly KEY_DATA_ID = 'id';
  public static readonly KEY_DATA_VERSION_ID = 'versionId';
  public static readonly KEY_DATA_CREATE_TIME = 'createTime';
  public static readonly KEY_DATA_PREVIOUS_ID = 'previousId';
  public static readonly KEY_DATA_NEXT_ID = 'nextId';

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

  public get [SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME](): string {
    return `${this.name}_Holder`;
  }

  public get [SYMBOL_DATA_MANAGER_DATA_TABLE_NAME](): string {
    return `${this.name}_Data`;
  }

  public getManager<M extends DataManager<M, D>, D extends Data<M, D>>(
    init: DataManagerConstructor<M, D>
  ): M {
    return this[SYMBOL_DATA_MANAGER_DATABASE].getManager(init as any) as unknown as M;
  }

  public get db(): Knex.Transaction<D, D[]> {
    return this[SYMBOL_DATA_MANAGER_TRANSACTION]();
  }

  protected abstract upgrade(table: Knex.AlterTableBuilder, oldVersion?: number): void;

  public async init(version?: number): Promise<void> {
    const exists = await this.db.raw(
      `select name from sqlite_master where type='table' and name='${this[SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME]}';`
    );

    if (exists.length == 0) {
      await this.db.schema.createTable(this[SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME], (table) => {
        table.increments(DataManager.KEY_HOLDER_ID);
        table.boolean(DataManager.KEY_HOLDER_DELETED).notNullable();
      });

      await this.db.schema.createTable(this[SYMBOL_DATA_MANAGER_DATA_TABLE_NAME], (table) => {
        table.increments(DataManager.KEY_DATA_VERSION_ID);
        table.integer(DataManager.KEY_DATA_ID).notNullable().index();
        table.integer(DataManager.KEY_DATA_CREATE_TIME).notNullable();
        table.integer(DataManager.KEY_DATA_PREVIOUS_ID).nullable();
        table.integer(DataManager.KEY_DATA_NEXT_ID).nullable();
      });
    }

    if (version == null || version < this.version) {
      for (let currentVersion = version ?? 0; currentVersion < this.version; currentVersion++) {
        await this.db.schema.alterTable(this[SYMBOL_DATA_MANAGER_DATA_TABLE_NAME], (table) => {
          this.upgrade(table, currentVersion);
        });
      }

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

  public async getById(id: number, options?: GetByIdOptions<M, D>): Promise<D | null> {
    const holder = await this.db
      .select('*')
      .from<DataHolder>(this[SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME])
      .where(DataManager.KEY_HOLDER_ID, '=', id)
      .where(DataManager.KEY_HOLDER_DELETED, '=', false)
      .first();

    if (holder == null) {
      return null;
    }

    if (options?.deleted != null ? holder.deleted !== options.deleted : holder.deleted) {
      return null;
    }

    let query = this.db
      .select('*')
      .from<D>(this[SYMBOL_DATA_MANAGER_DATA_TABLE_NAME])
      .where(DataManager.KEY_DATA_ID, '=', holder.id);

    if (options?.versionId != null) {
      query = query.where(DataManager.KEY_DATA_VERSION_ID, '=', options.versionId);
    }

    return <D | null>(
      ((await query.orderBy(DataManager.KEY_DATA_VERSION_ID, 'desc').first()) ?? null)
    );
  }

  public async listVersions(id: number): Promise<D[]> {
    const holder = await this.db
      .select('*')
      .from<DataHolder>(this[SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME])
      .where(DataManager.KEY_HOLDER_ID, '=', id)
      .first();

    if (holder == null) {
      return [];
    }

    return <D[]>(
      await this.db
        .select('*')
        .from<D>(this[SYMBOL_DATA_MANAGER_DATA_TABLE_NAME])
        .where(DataManager.KEY_DATA_ID, '=', holder[DataManager.KEY_HOLDER_ID])
    );
  }

  public async update(id: number, data: Partial<D>, options?: UpdateOptions<M, D>): Promise<D> {
    const latest = await this.getById(id);
    const baseVersion =
      options?.baseVersionId != null
        ? await this.getById(id, {
            versionId: options.baseVersionId
          })
        : latest;

    if (baseVersion == null) {
      throw new Error('Base version not found');
    }

    const newData = Object.assign<{}, D, Partial<D>, Partial<Data<M, D>>>({}, baseVersion, data, {
      [DataManager.KEY_DATA_PREVIOUS_ID]: latest?.[DataManager.KEY_DATA_VERSION_ID] ?? null
    });

    const result = await this.db
      .insert(<never>newData)
      .into<D>(this[SYMBOL_DATA_MANAGER_DATA_TABLE_NAME]);

    return <D>await this.getById(result[0]);
  }

  public async insert(data: Omit<D, keyof Data<never, never>>): Promise<D> {
    const resultHolder = await this.db
      .table<DataHolder>(this[SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME])
      .insert({ deleted: false });

    const holder = <DataHolder>(
      await this.db
        .select('*')
        .from<DataHolder>(this[SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME])
        .where(DataManager.KEY_HOLDER_ID, '=', resultHolder[0])
        .first()
    );

    const result = await this.db.table<D>(this[SYMBOL_DATA_MANAGER_DATA_TABLE_NAME]).insert(
      <never>Object.assign<{}, Omit<D, keyof Data<never, never>>, Partial<Data<M, D>>>({}, data, {
        [DataManager.KEY_DATA_ID]: holder[DataManager.KEY_HOLDER_ID],
        [DataManager.KEY_DATA_CREATE_TIME]: Date.now()
      })
    );

    return <D>(
      await this.db
        .table<D>(this[SYMBOL_DATA_MANAGER_DATA_TABLE_NAME])
        .where(DataManager.KEY_DATA_VERSION_ID, '=', result[0])
    );
  }

  public async delete(data: D): Promise<void> {
    await this.db
      .table<DataHolder>(this[SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME])
      .where(DataManager.KEY_HOLDER_ID, '=', data[DataManager.KEY_DATA_ID])
      .where(DataManager.KEY_HOLDER_DELETED, '=', false)
      .update({ deleted: true });
  }

  public async deleteWhere(whereClause?: WhereClause<M, D>[]): Promise<void> {
    const results = <D[]>await (whereClause ?? [])
      .reduce(
        (query, [columnName, operator, value]) =>
          query.where(columnName as any, operator, value as any),
        this.db.table<D>(this[SYMBOL_DATA_MANAGER_DATA_TABLE_NAME]).select('*')
      )
      .where(DataManager.KEY_DATA_NEXT_ID, '=', null);

    for (const result of results) {
      await this.delete(result);
    }
  }

  public async purge(id: number): Promise<void> {
    await this.db
      .table<D>(this[SYMBOL_DATA_MANAGER_DATA_TABLE_NAME])
      .where(DataManager.KEY_DATA_ID, '=', id)
      .del();

    await this.db
      .table<DataHolder>(this[SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME])
      .where(DataManager.KEY_HOLDER_ID, '=', id)
      .del();
  }

  public async query(options?: QueryOptions<M, D>): Promise<D[]> {
    let currentOffset = options?.offset ?? 0;

    const results: D[] = [];

    while (results.length < (options?.limit ?? Infinity)) {
      let query = (options?.where ?? []).reduce(
        (query, [columnName, operator, value]) =>
          query.where(columnName as any, operator, value as any),

        this.db.select('*').from<D>(this[SYMBOL_DATA_MANAGER_DATA_TABLE_NAME])
      );

      query.where(DataManager.KEY_DATA_NEXT_ID, '=', null);

      if (options?.limit != null) {
        query = query.limit(options.limit - results.length);
      }

      if (options?.offset != null) {
        query = query.offset(currentOffset);
      }

      const entres = <D[]>await query;
      if (entres.length === 0) {
        break;
      }

      for (const entry of entres) {
        const holder = await this.db
          .select('*')
          .from<DataHolder>(this[SYMBOL_DATA_MANAGER_HOLDER_TABLE_NAME])
          .where(DataManager.KEY_HOLDER_ID, '=', entry[DataManager.KEY_DATA_ID])
          .first();

        if (holder == null) {
          continue;
        }

        if (options?.deleted != null ? holder.deleted !== options.deleted : holder.deleted) {
          continue;
        }

        results.push(entry);
      }

      if (options?.limit != null) {
        currentOffset += options.limit;
      }
    }

    return results;
  }
}

export interface UpdateOptions<M extends DataManager<M, D>, D extends Data<M, D>> {
  baseVersionId?: number;
}

export interface DeleteOptions<M extends DataManager<M, D>, D extends Data<M, D>> {
  where?: WhereClause<M, D>[];
}

export interface GetByIdOptions<M extends DataManager<M, D>, D extends Data<M, D>> {
  deleted?: boolean;
  versionId?: number;
}

export interface QueryOptions<M extends DataManager<M, D>, D extends Data<M, D>> {
  where?: WhereClause<M, D>[];
  orderBy?: OrderByClause<M, D>[];

  offset?: number;
  limit?: number;

  deleted?: boolean;
}

export type WhereClause<
  M extends DataManager<M, D>,
  D extends Data<M, D>,
  T extends keyof D = keyof D
> = [T, '=' | '>' | '>=' | '<' | '<=' | '<>', D[T]];

export type OrderByClause<
  M extends DataManager<M, D>,
  D extends Data<M, D>,
  T extends keyof D = keyof D
> = [T, 'asc' | 'desc'];