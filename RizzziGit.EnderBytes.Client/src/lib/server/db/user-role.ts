import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import type { User } from './user';

export enum UserRoleType {
  SiteAdmin,
  SiteTechnician
}

export interface UserRole extends Data<UserRoleManager, UserRole> {
  userId: number;
  type: UserRoleType;
}

export class UserRoleManager extends DataManager<UserRoleManager, UserRole> {
  public static readonly NAME = 'UserRole';
  public static readonly VERSION = 1;

  public static readonly KEY_USER_ID = 'userId';
  public static readonly KEY_TYPE = 'type';

  public constructor(db: Database, transaction: () => Knex.Transaction<UserRole>) {
    super(db, transaction, UserRoleManager.NAME, UserRoleManager.VERSION);
  }

  protected async upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): Promise<void> {
    if (oldVersion < 1) {
      table.integer(UserRoleManager.KEY_USER_ID).notNullable();
      table.integer(UserRoleManager.KEY_TYPE).notNullable();
    }
  }

  public async list(): Promise<UserRole[]> {
    return await this.db.select('*').from<UserRole>(this.name);
  }

  public async count(): Promise<number> {
    const result = (await this.db.from<UserRole>(this.name).count())[0];

    return result['count(*)'] as number;
  }

  public async create(user: User, type: UserRoleType): Promise<UserRole> {
    return this.insert({
      userId: user.id,
      type
    });
  }

  public deleteAllFromUser(user: User) {
    return this.deleteWhere([[UserRoleManager.KEY_USER_ID, '=', user.id]]);
  }
}

Database.register(UserRoleManager);
