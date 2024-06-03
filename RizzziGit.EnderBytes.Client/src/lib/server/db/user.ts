import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { UserKeyManager, type UnlockedUserKey } from './user-key';
import { UserSessionManager } from './user-session';
import {
  UserKeyType,
  UserRole,
  UsernameVerificationFlag,
  usernameLength,
  usernameValidCharacters
} from '$lib/shared/db';

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

  protected get ftsColumns(): (keyof User)[] {
    return [
      UserManager.KEY_USERNAME,
      UserManager.KEY_FIRST_NAME,
      UserManager.KEY_LAST_NAME,
      UserManager.KEY_MIDDLE_NAME
    ];
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

  public async verify(
    username: string,
    checkExisting: boolean = true
  ): Promise<UsernameVerificationFlag> {
    let flag: UsernameVerificationFlag = UsernameVerificationFlag.OK;

    const [min, max] = usernameLength;

    if (username.length < min || username.length > max) {
      flag |= UsernameVerificationFlag.InvalidLength;
    }

    if (!username.match(new RegExp(`^[${usernameValidCharacters}]+$`, 'g'))) {
      flag |= UsernameVerificationFlag.InvalidCharacters;
    }

    if (checkExisting) {
      if ((await this.queryCount([[UserManager.KEY_USERNAME, '=', username]])) > 0) {
        flag |= UsernameVerificationFlag.Taken;
      }
    }

    return flag;
  }

  public async create(
    username: string,
    firstName: string,
    middleName: string | null,
    lastName: string,
    password: string = this.generateRandomPassword(16),
    role: UserRole = UserRole.Member
  ): Promise<[user: User, unlockedUserKey: UnlockedUserKey, password: string]> {
    if ((await this.verify(username)) !== UsernameVerificationFlag.OK) {
      throw new Error('Invalid username');
    }

    const user = await this.insert({
      [UserManager.KEY_USERNAME]: username,
      [UserManager.KEY_FIRST_NAME]: firstName,
      [UserManager.KEY_MIDDLE_NAME]: middleName,
      [UserManager.KEY_LAST_NAME]: lastName,
      [UserManager.KEY_ROLE]: role,
      [UserManager.KEY_IS_SUSPENDED]: false
    });

    const userKeyManager = this.getManager(UserKeyManager);
    const userKey = await userKeyManager.create(
      user,
      UserKeyType.Password,
      new TextEncoder().encode(password)
    );

    return [user, userKey, password];
  }

  public async delete(user: User) {
    const [userKeys, userSessions] = this.getManagers(UserKeyManager, UserSessionManager);

    await Promise.all([
      userKeys.deleteWhere([[UserKeyManager.KEY_USER_ID, '=', user.id]]),
      userSessions.deleteWhere([[UserSessionManager.KEY_USER_ID, '=', user.id]])
    ]);

    await super.delete(user);
  }

  public async getByUsername(username: string): Promise<User | null> {
    if ((await this.verify(username, false)) !== UsernameVerificationFlag.OK) {
      return null;
    }

    return (
      (
        await this.query({
          where: [[UserManager.KEY_USERNAME, '=', username]],
          limit: 1
        })
      )[0] ?? null
    );
  }

  public async update(oldUser: User, user: UpdateUserOptions): Promise<User> {
    const username = user[UserManager.KEY_USERNAME];

    if (username != null) {
      const usernameVerification = await this.verify(username);

      if (usernameVerification !== UsernameVerificationFlag.OK) {
        throw new Error('Invalid username');
      }
    }

    return await super.update(oldUser, user);
  }

  public async suspend(user: User): Promise<User> {
    return await super.update(user, { [UserManager.KEY_IS_SUSPENDED]: true });
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
