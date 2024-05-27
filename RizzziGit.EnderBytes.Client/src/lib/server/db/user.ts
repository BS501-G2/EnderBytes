import type { Knex } from 'knex';
import { DataManager, Database, type Data } from '../db';
import { UserKeyManager, UserKeyType, generateKeyPair, type UnlockedUserKey } from './user-key';

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

  protected async upgrade(oldVersion: number = 0): Promise<void> {
    if (oldVersion < 1) {
      await this.db.schema.alterTable(this.name, (table) => {
        table.string(UserManager.KEY_USERNAME).unique().collate('nocase');
        table.string(UserManager.KEY_FIRST_NAME).notNullable();
        table.string(UserManager.KEY_MIDDLE_NAME).nullable();
        table.string(UserManager.KEY_LAST_NAME).notNullable();
      });
    }
  }

  public async create(
    username: string,
    firstName: string,
    middleName: string | null,
    lastName: string,
    password: string
  ): Promise<[User, UnlockedUserKey]> {
    const userManager = this.getManager(UserKeyManager);

    const result = await this.db
      .insert(this.new({ username, firstName, middleName, lastName }))
      .into(this.name);

    const user = <User>await this.get(result[0]);
    console.log(user)
    const [privateKey, publicKey] = await generateKeyPair();
    const userKey = await userManager.create(
      user,
      privateKey,
      publicKey,
      UserKeyType.Password,
      Buffer.from(password)
    );

    return [user, userKey];
  }
}

Database.register(UserManager);
