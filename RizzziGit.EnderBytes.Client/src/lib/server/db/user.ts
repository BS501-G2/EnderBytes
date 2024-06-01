import type { Knex } from 'knex';
import { DataManager, Database, type Data, type QueryOptions } from '../db';
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
  [UserManager.KEY_IS_SUSPENDED]: boolean;
}

export class UserManager extends DataManager<UserManager, User> {
  public static readonly KEY_USERNAME = 'username';
  public static readonly KEY_FIRST_NAME = 'firstName';
  public static readonly KEY_MIDDLE_NAME = 'middleName';
  public static readonly KEY_LAST_NAME = 'lastName';
  public static readonly KEY_ROLE = 'role';
  public static readonly KEY_IS_SUSPENDED = 'isSuspended';

  public constructor(db: Database, transaction: () => Knex.Transaction<User>) {
    super(db, transaction, 'User', 1);
  }

  protected async upgrade(table: Knex.AlterTableBuilder, oldVersion: number = 0): Promise<void> {
    if (oldVersion < 1) {
      table.string(UserManager.KEY_USERNAME).collate('nocase');
      table.string(UserManager.KEY_FIRST_NAME).notNullable();
      table.string(UserManager.KEY_MIDDLE_NAME).nullable();
      table.string(UserManager.KEY_LAST_NAME).notNullable();
      table.integer(UserManager.KEY_ROLE).notNullable();
      table.boolean(UserManager.KEY_IS_SUSPENDED).notNullable();
    }
  }

  public readonly randomPasswordMap: string =
    'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789;,./<>?`~!@#$%^&*()_+-=[]{}|\\:"';

  public generateRandomPassword(length: number): string {
    let password = '';
    for (let i = 0; i < length; i++) {
      password += this.randomPasswordMap[Math.floor(Math.random() * this.randomPasswordMap.length)];
    }
    return password;
  }

  public async create(
    username: string,
    firstName: string,
    middleName: string | null,
    lastName: string,
    password: string = this.generateRandomPassword(16),
    role: UserRole = UserRole.Member
  ): Promise<[user: User, unlockedUserKey: UnlockedUserKey, password: string]> {
    const userKeyManager = this.getManager(UserKeyManager);

    const user = await this.insert({
      [UserManager.KEY_USERNAME]: username,
      [UserManager.KEY_FIRST_NAME]: firstName,
      [UserManager.KEY_MIDDLE_NAME]: middleName,
      [UserManager.KEY_LAST_NAME]: lastName,
      [UserManager.KEY_ROLE]: role,
      [UserManager.KEY_IS_SUSPENDED]: false
    });

    const userKey = await userKeyManager.create(
      user,
      UserKeyType.Password,
      new TextEncoder().encode(password)
    );

    return [user, userKey, password];
  }

  public async delete(user: User) {
    const [userKeys, userSessions, keyShards] = this.getManagers(
      UserKeyManager,
      UserSessionManager,
      KeyShardManager
    );

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

  public async update(id: number, user: UpdateUserOptions): Promise<User> {
    const username = user[UserManager.KEY_USERNAME];
    if (username != null) {
      const existingUser = (
        await this.query({
          where: [
            [UserManager.KEY_USERNAME, '=', username],
            [UserManager.KEY_DATA_ID, '!=', id]
          ]
        })
      )[0];

      if (existingUser != null && existingUser[UserManager.KEY_DATA_ID] !== id) {
        throw new Error('Username already in use');
      }
    }

    return await super.update(id, user);
  }

  public async suspend(id: number): Promise<User> {
    return await super.update(id, { [UserManager.KEY_IS_SUSPENDED]: true });
  }
}

Database.register(UserManager);

export interface UpdateUserOptions extends Partial<User> {
  [UserManager.KEY_USERNAME]?: string;
  [UserManager.KEY_FIRST_NAME]?: string;
  [UserManager.KEY_MIDDLE_NAME]?: string | null;
  [UserManager.KEY_LAST_NAME]?: string;
  [UserManager.KEY_ROLE]?: UserRole;
}
