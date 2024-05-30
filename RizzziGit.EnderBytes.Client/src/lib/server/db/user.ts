import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { UserKeyManager, type UnlockedUserKey } from './user-key';
import { UserSessionManager } from './user-session';
import { UserKeyType, UserRole } from '$lib/shared/db';
import { KeyShardManager } from './key-shard';

export interface User extends Data<UserManager, User> {
  [UserManager.KEY_USERNAME]: string;
  [UserManager.KEY_FIRST_NAME]: string;
  [UserManager.KEY_MIDDLE_NAME]: string | null;
  [UserManager.KEY_LAST_NAME]: string;
  [UserManager.KEY_ROLE]: UserRole;
}

export class UserManager extends DataManager<UserManager, User> {
  public static readonly KEY_USERNAME = 'username';
  public static readonly KEY_FIRST_NAME = 'firstName';
  public static readonly KEY_MIDDLE_NAME = 'middleName';
  public static readonly KEY_LAST_NAME = 'lastName';
  public static readonly KEY_ROLE = 'role';

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
    password: string,
    role: UserRole = UserRole.Member
  ): Promise<[User, UnlockedUserKey]> {
    const userKeyManager = this.getManager(UserKeyManager);

    const user = await this.insert({
      username,
      firstName,
      middleName,
      lastName,
      role
    });

    const userKey = await userKeyManager.create(
      user,
      UserKeyType.Password,
      new TextEncoder().encode(password)
    );

    return [user, userKey];
  }

  public async delete(user: User) {
    const [userKeys, userSessions, keyShards] = this.getManagers(UserKeyManager, UserSessionManager, KeyShardManager);

    await Promise.all([
      userKeys.deleteWhere([[UserKeyManager.KEY_USER_ID, '=', user.id]]),
      userSessions.deleteWhere([[UserSessionManager.KEY_USER_ID, '=', user.id]]),
      keyShards.deleteWhere([[KeyShardManager.KEY_USER_ID, '=', user.id]])
    ]);

    await super.delete(user);
  }

  public async getByUsername(username: string): Promise<User | null> {
    return (
      (
        await this.query({
          where: [[UserManager.KEY_USERNAME, '=', username]],
          limit: 1
        })
      )[0] ?? null
    );
  }
}

Database.register(UserManager);
