import knex, { Knex,} from 'knex';
import { Database, ResourceManager, type ResourceData } from '../database';

export enum UserColumn {
	Username = 'username',
	LastName = 'lastName',
	FirstName = 'firstName',
	MiddleName = 'middleName',
	PublicKey = 'publicKey'
}

export interface User extends ResourceData<User, UserManager> {
	[UserColumn.Username]: string;
	[UserColumn.LastName]: string;
	[UserColumn.FirstName]: string;
	[UserColumn.MiddleName]: string | null;
	[UserColumn.PublicKey]: Buffer;
}

export class UserManager extends ResourceManager<User, UserManager> {
	public constructor(database: Database) {
		super(database);
	}

	public get name(): string {
		return 'User';
	}

	public get version(): number {
		return 1;
	}

	protected override async init(db: Knex.Transaction, oldVersion: number): Promise<void> {
		const { name } = this;

		if (oldVersion < 1) {
			await db.schema.alterTable(name, (table) => {
				table.string(UserColumn.Username, 16).notNullable().index(`index_${name}_username`);
				table.string(UserColumn.LastName, 32).notNullable();
				table.string(UserColumn.FirstName, 32).notNullable();
				table.string(UserColumn.MiddleName, 32).nullable();
				table.binary(UserColumn.PublicKey, 4096).notNullable();
			});
		}
	}
}
