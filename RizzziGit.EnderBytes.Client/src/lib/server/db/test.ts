import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';

export interface TestData extends Data<TestManager, TestData> {
  [TestManager.KEY_TEST]: string;
}

export class TestManager extends DataManager<TestManager, TestData> {
  public static readonly NAME = 'Test';
  public static readonly VERSION = 1;

  public static readonly KEY_TEST = 'test';

  public constructor(db: Database, transaction: () => Knex.Transaction<TestData>) {
    super(db, transaction, TestManager.NAME, TestManager.VERSION);
  }

  protected upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): void {
    if (oldVersion < 1) {
      table.string(TestManager.KEY_TEST).notNullable();
    }
  }

  public async create(test: string): Promise<TestData> {
    return this.insert({ test });
  }

  public get ftsColumns(): (keyof TestData)[] {
    return [TestManager.KEY_TEST];
  }
}

Database.register(TestManager);
