import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { UserKeyManager, UserKeyType, generateKeyPair, type UnlockedUserKey } from './user-key';
import { UserRoleManager } from './user-role';
import { UserSessionManager } from './user-session';

export interface User extends Data<UserManager, User> {
  username: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
}

export class UserManager extends DataManager<UserManager, User> {
  public static readonly KEY_USERNAME = 'username';
  public static readonly KEY_FIRST_NAME = 'firstName';
  public static readonly KEY_MIDDLE_NAME = 'middleName';
  public static readonly KEY_LAST_NAME = 'lastName';

  public constructor(db: Database, transaction: () => Knex.Transaction<User>) {
    super(db, transaction, 'User', 1);
  }

  protected async upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): Promise<void> {
    if (oldVersion < 1) {
      table.string(UserManager.KEY_USERNAME).unique().collate('nocase');
      table.string(UserManager.KEY_FIRST_NAME).notNullable();
      table.string(UserManager.KEY_MIDDLE_NAME).nullable();
      table.string(UserManager.KEY_LAST_NAME).notNullable();
    }
  }

  public async create(
    username: string,
    firstName: string,
    middleName: string | null,
    lastName: string,
    password: string
  ): Promise<[User, UnlockedUserKey]> {
    const userKeyManager = this.getManager(UserKeyManager);

    const user = await this.insert({
      username,
      firstName,
      middleName,
      lastName
    });
    const [privateKey, publicKey] = await generateKeyPair();
    const userKey = await userKeyManager.create(
      user,
      privateKey,
      publicKey,
      UserKeyType.Password,
      Buffer.from(password)
    );

    return [user, userKey];
  }

  public async delete(user: User) {
    const userRoleManager = this.getManager(UserRoleManager);
    const userKeyManager = this.getManager(UserKeyManager);
    const userSessionManager = this.getManager(UserSessionManager);

    await userSessionManager.deleteAllFromUser(user);
    await userKeyManager.deleteAllFromUser(user);
    await userRoleManager.deleteAllFromUser(user);

    await super.delete(user);
  }
}

Database.register(UserManager);
