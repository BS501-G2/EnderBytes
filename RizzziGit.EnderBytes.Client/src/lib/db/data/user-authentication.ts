import type { Knex } from 'knex';
import { ResourceManager, type Database, type ResourceData } from '../database';
import type { User, UserManager } from './user';
import Crypto from 'crypto';

export enum UserAuthenticationType {
	Password = 0
}

export enum UserAuthenticationColumn {
	UserId = 'userId',
	Type = 'type',
	Salt = 'salt',
	Iterations = 'iterations',
	TestBytes = 'challengeBytes',
	TestBytesEncrypted = 'challengeEncryptedBytes',
	PrivateKey = 'encryptedPrivateKey',
	PublicKey = 'publicKey'
}

export interface UserToken {
	user: User;
	userAuthentication: UserAuthentication;

	payload: Buffer;
}

export interface UserAuthentication extends ResourceData<UserAuthentication, UserManager> {
	[UserAuthenticationColumn.UserId]: number;

	[UserAuthenticationColumn.Type]: UserAuthenticationType;
	[UserAuthenticationColumn.Salt]: Buffer;
	[UserAuthenticationColumn.Iterations]: number;

	[UserAuthenticationColumn.TestBytes]: Buffer;
	[UserAuthenticationColumn.TestBytesEncrypted]: Buffer;

	[UserAuthenticationColumn.PrivateKey]: Buffer;
	[UserAuthenticationColumn.PublicKey]: Buffer;
}

export class UserAuthenticationManager extends ResourceManager<UserAuthentication, UserManager> {
	public constructor(database: Database) {
		super(database);
	}

	public get name(): string {
		return 'UserAuthentication';
	}

	public get version(): number {
		return 1;
	}

	protected async init(db: Knex.Transaction<any, any[]>, oldVersion: number): Promise<void> {
		const { name } = this;

		if (oldVersion < 1) {
			await db.schema.alterTable(name, (table) => {
				table
					.integer(UserAuthenticationColumn.UserId)
					.notNullable()
					.unsigned()
					.index(`index_${name}_userId`);
				table.integer(UserAuthenticationColumn.Type).notNullable();
				table.binary(UserAuthenticationColumn.Salt, 32).notNullable();
				table.integer(UserAuthenticationColumn.Iterations).notNullable();
				table.binary(UserAuthenticationColumn.TestBytes, 4096).notNullable();
				table.binary(UserAuthenticationColumn.TestBytesEncrypted, 4096).notNullable();
				table.binary(UserAuthenticationColumn.PrivateKey, 4096).notNullable();
				table.binary(UserAuthenticationColumn.PublicKey, 4096).notNullable();
			});
		}
	}

	public async create(
		db: Knex.Transaction,
		user: User,
		type: UserAuthenticationType,
		payload: ArrayBuffer,
		privateKey: ArrayBuffer
	): Promise<UserAuthentication> {
		await new Promise<{ publicKey: Crypto.KeyObject; privateKey: Crypto.KeyObject }>(
			(resolve, reject) => {
				Crypto.generateKeyPair('rsa', { modulusLength: 4096 }, (error, publicKey, privateKey) => {
					if (error) {
						reject(error);
					} else {
						resolve({ publicKey, privateKey });
					}
				});
			}
		);

		const randomBytes = Crypto.randomBytes(1024 * 4);
	}
}
