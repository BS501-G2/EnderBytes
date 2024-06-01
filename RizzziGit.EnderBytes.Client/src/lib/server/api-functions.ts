import {
  ApiError,
  ApiErrorType,
  type Authentication,
  type AuthenticationRequest,
  type ServerStatus
} from '$lib/shared/api';
import { UserKeyType, UserRole } from '$lib/shared/db';
import {
  Database,
  DataManager,
  type Data,
  type DataManagerConstructor,
  type DataManagerConstructorInstance,
  type QueryOptions
} from './db';
import { UserManager, type UpdateUserOptions, type User } from './db/user';
import { UserKeyManager, type UnlockedUserKey } from './db/user-key';
import { UserSessionManager } from './db/user-session';
import { randomBytes } from './utils';

function getManager<M extends DataManager<M, D>, D extends Data<M, D>>(
  init: DataManagerConstructor<M, D>
): Promise<M> {
  return Database.getInstance().then((database) => database.getManager(init));
}

async function getManagers<C extends readonly DataManagerConstructor<any, any>[]>(
  ...init: C
): Promise<{ [K in keyof C]: DataManagerConstructorInstance<C[K]> }> {
  return Database.getInstance().then((database) => database.getManagers(...init));
}

async function requireAuthentication(
  authentication: AuthenticationRequest | null
): Promise<UnlockedUserKey> {
  if (authentication == null) {
    ApiError.throw(ApiErrorType.Unauthorized);
  }

  const [users, userKeys, userSessions] = await getManagers(
    UserManager,
    UserKeyManager,
    UserSessionManager
  );
  const { userSessionAuthTag, userSessionId } = authentication;

  const userSession = await userSessions.getById(userSessionId);
  if (userSession == null) {
    ApiError.throw(ApiErrorType.Unauthorized);
  }

  const user = await users.getById(userSession[UserSessionManager.KEY_USER_ID]);
  if (user == null) {
    ApiError.throw(ApiErrorType.Unauthorized);
  }

  if (user[UserManager.KEY_IS_SUSPENDED]) {
    ApiError.throw(
      ApiErrorType.Forbidden,
      `User @${user[UserManager.KEY_USERNAME]} is currently suspended.`
    );
  }

  if (userSession[UserSessionManager.KEY_EXPIRE_TIME] < Date.now()) {
    ApiError.throw(ApiErrorType.Unauthorized);
  }

  const unlockedUserSessions = userSessions.unlock(userSession, userSessionAuthTag);

  const userKey = await userKeys.getById(
    unlockedUserSessions[UserSessionManager.KEY_ORIGIN_KEY_ID]
  );
  if (userKey == null) {
    ApiError.throw(ApiErrorType.Unauthorized);
  }

  return userSessions.unlockKey(unlockedUserSessions, userKey);
}

async function ensureUserRole(unlockedUserKey: UnlockedUserKey, type: UserRole): Promise<void> {
  const [users] = await getManagers(UserManager);
  const user = await users.getById(unlockedUserKey.userId);
  if (user == null) {
    ApiError.throw(ApiErrorType.Forbidden);
  }

  if (user[UserManager.KEY_ROLE] < type) {
    ApiError.throw(ApiErrorType.Forbidden);
  }
}

export async function random(size: number): Promise<Uint8Array> {
  const bytes = await randomBytes(size);

  return bytes;
}

export function echo<T extends any>(data: T): T {
  return data;
}

export async function getServerStatus(): Promise<ServerStatus> {
  const users = await getManager(UserManager);

  const result = {
    setupRequired:
      (await users.queryCount([[UserManager.KEY_ROLE, '>=', UserRole.SiteAdmin]])) === 0
  };

  return result;
}

export async function createAdminUser(
  username: string,
  firstName: string,
  middleName: string | null,
  lastName: string,
  password: string
): Promise<User> {
  const [users] = await getManagers(UserManager);

  const status = await getServerStatus();
  if (!status.setupRequired) {
    throw new Error('Admin user already exists');
  }

  const [user, unlockedUserKey] = await users.create(
    username,
    firstName,
    middleName,
    lastName,
    password,
    UserRole.SiteAdmin
  );

  return user;
}

export async function autenticate(
  username: string,
  userPayloadType: UserKeyType,
  payload: Uint8Array
): Promise<Authentication> {
  const [users, userSessions, userKeys] = await getManagers(
    UserManager,
    UserSessionManager,
    UserKeyManager
  );

  const user = await users.getByUsername(username);
  if (user == null) {
    ApiError.throw(ApiErrorType.InvalidRequest, `User ${username} does not exist.`);
  }

  if (user[UserManager.KEY_IS_SUSPENDED]) {
    ApiError.throw(
      ApiErrorType.Forbidden,
      `User @${user[UserManager.KEY_USERNAME]} is currently suspended.`
    );
  }

  const unlockedUserKey = await userKeys.findByPayload(user, userPayloadType, payload);
  if (unlockedUserKey == null) {
    ApiError.throw(ApiErrorType.InvalidRequest, `Invalid authentication payload.`);
  }

  const unlockedSession = await userSessions.create(unlockedUserKey);

  return {
    userId: user.id,
    userSessionId: unlockedSession.id,
    userSessionKey: unlockedSession[UserSessionManager.KEY_KEY],
    userSessionIv: unlockedSession[UserSessionManager.KEY_IV],
    userSessionAuthTag: unlockedSession[UserSessionManager.KEY_UNLOCKED_AUTH_TAG]
  };
}

export async function validateAuthentication(authentication: AuthenticationRequest): Promise<boolean> {
  try {
    await requireAuthentication(authentication);
    return true;
  } catch {
    return false;
  }
}

export async function getUser(
  authentication: AuthenticationRequest | null,
  idOrUsername: number | string
): Promise<User | null> {
  await requireAuthentication(authentication);

  const [users] = await getManagers(UserManager);

  if (typeof idOrUsername === 'string') {
    return await users.getByUsername(idOrUsername);
  } else if (typeof idOrUsername === 'number') {
    return await users.getById(idOrUsername);
  } else {
    ApiError.throw(ApiErrorType.InvalidRequest, 'Invalid id or username type');
  }
}

export async function listUsers(
  authentication: AuthenticationRequest | null,
  options?: QueryOptions<UserManager, User>
): Promise<User[]> {
  await requireAuthentication(authentication);

  const [users] = await getManagers(UserManager);

  return await users.query(options);
}

export async function createUser(
  authentication: AuthenticationRequest | null,
  username: string,
  firstName: string,
  middleName: string | null,
  lastName: string,
  role: UserRole
): Promise<[user: User, unlockedUserKey: UnlockedUserKey, password: string]> {
  const unlockUserKey = await requireAuthentication(authentication);
  await ensureUserRole(unlockUserKey, UserRole.SiteAdmin);

  const [users] = await getManagers(UserManager);
  const [user, unlockedUserKey, password] = await users.create(
    username,
    firstName,
    middleName,
    lastName,
    undefined,
    role
  );

  return [user, unlockedUserKey, password];
}

export async function updateUser(
  authentication: AuthenticationRequest | null,
  id: number,
  newData: UpdateUserOptions
): Promise<User> {
  const unlockedUserKey = await requireAuthentication(authentication);

  const [users] = await getManagers(UserManager);
  const user = await users.getById(id);
  if (user == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }

  if (unlockedUserKey[UserKeyManager.KEY_USER_ID] !== user.id) {
    ApiError.throw(ApiErrorType.Forbidden);
  }

  const result = await users.update(id, newData);
  return result;
}

export async function suspendUser(
  authentication: AuthenticationRequest | null,
  id: number
): Promise<User> {
  const unlockedUserKey = await requireAuthentication(authentication);
  await ensureUserRole(unlockedUserKey, UserRole.SiteAdmin);

  const [users] = await getManagers(UserManager);
  const user = await users.getById(id);
  if (user == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }

  return await users.suspend(id);
}
