import knex, { Knex } from 'knex';
import { UserManager } from './data/user';
import { UserAuthenticationManager } from './data/user-authentication';

const DatabaseInitFunctionSymbol = Symbol('Database.Init');
const DatabaseRegisterResourceManagersFunctionSymbol = Symbol('Database.ResourceManagers');

const ResourceManagerInitFunctionSymbol = Symbol('ResourceManager.Init');

export class Database {
	public constructor(server: string, database: string, user: string, password: string) {
		this.#knex = knex({
			client: 'mysql2',
			connection: {
				host: server,
				user: user,
				password: password,
				database: database
			}
		});

		this.#resourceManagers = new Set();
		this.users = new UserManager(this);
		this.userAuthentications = new UserAuthenticationManager(this);
	}

	readonly #knex: Knex;
	readonly #resourceManagers: Set<ResourceManager<any, any>>;

	public readonly users: UserManager;
	public readonly userAuthentications: UserAuthenticationManager;

	public [DatabaseRegisterResourceManagersFunctionSymbol](manager: ResourceManager<any, any>) {
		this.#resourceManagers.add(manager);
	}

	protected async transact<T>(handler: (db: Knex.Transaction) => T | Promise<T>): Promise<T> {
		const transaction = await this.#knex.transaction();
		try {
			const result = await handler(transaction);
			await transaction.commit();
			return result;
		} catch (error) {
			await transaction.rollback();
			throw error;
		}
	}

	public async [DatabaseInitFunctionSymbol]() {
		this.transact(async (db) => {
			const { schema } = db;

			await schema.createTableIfNotExists('versioning', (table) => {
				table.string('name').primary();
				table.integer('version').notNullable();
			});

			for (const manager of this.#resourceManagers) {
				const version = await db
					.select<VersioningRecord>('version')
					.from('versioning')
					.where('name', manager.name)
					.first();

				if (version?.version !== manager.version) {
					const { name, version } = manager;

					await manager[ResourceManagerInitFunctionSymbol](db, version ?? 0);

					if (version == null) {
						await db.insert({ name, version }).into('versioning');
					} else {
						await db.update({ version }).from('versioning').where('name', name);
					}
				}
			}
		});
	}
}

export interface VersioningRecord {
	name: string;
	version: number;
}

let databaseInstance: Database | null = $state(null);
export async function getDatabase(): Promise<Database> {
	if (databaseInstance !== null) {
		return databaseInstance;
	}

	databaseInstance = new Database('10.0.0.3', 'enderbytes', 'enderbytes', 'enderbytes');
	await databaseInstance[DatabaseInitFunctionSymbol]();
	return databaseInstance;
}

export interface ResourceData<D extends ResourceData<D, M>, M extends ResourceManager<D, M>> {
	id: number;
	createTime: number;
	updateTime: number;
}

export abstract class ResourceManager<
	D extends ResourceData<D, M>,
	M extends ResourceManager<D, M>
> {
	public constructor(database: Database) {
		this.#database = database;
		this.#database[DatabaseRegisterResourceManagersFunctionSymbol](this);
	}

	readonly #database: Database;

	public get database() {
		return this.#database;
	}
	public abstract get name(): string;
	public abstract get version(): number;

	public async [ResourceManagerInitFunctionSymbol](
		db: Knex.Transaction,
		oldVersion: number = 0
	): Promise<void> {
		const { name } = this;

		await db.schema.createTableIfNotExists(name, (table) => {
			table.increments('id').primary();
			table.timestamp('createTime').defaultTo(db.fn.now());
			table.timestamp('updateTime').defaultTo(db.fn.now());
		});

		await this.init(db, oldVersion);
	}

	protected abstract init(transaction: Knex.Transaction, oldVersion: number): Promise<void>;
}

export class TransactingResourceManager {}
