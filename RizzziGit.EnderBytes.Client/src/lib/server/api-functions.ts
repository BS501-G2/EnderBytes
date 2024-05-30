import {
  ApiError,
  ApiErrorType,
  userSessionExpiryDuration,
  type Authentication
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
import { UserManager, type User } from './db/user';
import { UserKeyManager, type UnlockedUserKey } from './db/user-key';
import { UserSessionManager } from './db/user-session';

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
  authentication: Authentication | null
): Promise<UnlockedUserKey> {
  if (authentication == null) {
    ApiError.throw(ApiErrorType.Unauthorized);
  }

  const [userSessions, userKeys] = await getManagers(UserSessionManager, UserKeyManager);
  const { userSessionId, userSessionKey, userKeyId } = authentication;

  const userSession = await userSessions.getById(userSessionId);
  if (userSession == null) {
    ApiError.throw(ApiErrorType.Unauthorized);
  }

  const userKey = await userKeys.getById(userKeyId);
  if (userKey == null) {
    ApiError.throw(ApiErrorType.Unauthorized);
  }

  try {
    const unlockedUserSession = await userSessions.unlock(
      userSession,
      Buffer.from(userSessionKey.buffer)
    );

    return userSessions.unlockUserKey(unlockedUserSession, userKey);
  } catch (error: any) {
    ApiError.throwFrom(error, ApiErrorType.Unauthorized);
  }
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

export function echo<T>(data: T): T {
  return data;
}

export async function getServerStatus(): Promise<{
  setupRequired: boolean;
}> {
  const users = await getManager(UserManager);

  return {
    setupRequired:
      (await users.queryCount([[UserManager.KEY_ROLE, '>=', UserRole.SiteAdmin]])) === 0
  };
}

export async function createAdminUser(request: {
  username: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
  password: string;
}): Promise<User> {
  const [users] = await getManagers(UserManager);

  const status = await getServerStatus();
  if (!status.setupRequired) {
    throw new Error('Admin user already exists');
  }

  const [user, unlockedUserKey] = await users.create(
    request.username,
    request.firstName,
    request.middleName,
    request.lastName,
    request.password
  );

  return user;
}

export async function autenticate(
  username: string,
  userPayloadType: UserKeyType,
  payload: Uint8Array
): Promise<Authentication | null> {
  const [users, userSessions, userKeys] = await getManagers(
    UserManager,
    UserSessionManager,
    UserKeyManager
  );

  const user = await users.getByUsername(username);
  if (user == null) {
    return null;
  }

  const userKeyList = await userKeys.findByPayload(
    user,
    userPayloadType,
    Buffer.from(payload.buffer)
  );

  if (userKeyList == null) {
    return null;
  }

  const userSession = await userSessions.create(
    user,
    userKeyList,
    Date.now() + userSessionExpiryDuration
  );

  return {
    userSessionId: userSession.id,
    userSessionKey: userSession.key,
    userKeyId: userKeyList.id
  };
}

export async function verify(authentication: Authentication): Promise<boolean> {
  try {
    await requireAuthentication(authentication);

    return true;
  } catch {
    return false;
  }
}

export async function listUsers(
  authentication: Authentication | null,
  options?: QueryOptions<UserManager, User>
): Promise<User[]> {
  const unlockUserKey = await requireAuthentication(authentication);
  await ensureUserRole(unlockUserKey, UserRole.SiteAdmin);

  const [users] = await getManagers(UserManager);

  return await users.query(options);
}
