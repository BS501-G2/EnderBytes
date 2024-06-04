import { ApiError, ApiErrorType, type Authentication, type ServerStatus } from '$lib/shared/api';
import { FileAccessLevel, FileType, UserRole, type UserKeyType } from '$lib/shared/db';
import {
  Database,
  type Data,
  type DataManager,
  type DataManagerConstructor,
  type DataManagerConstructorInstance,
  type QueryOptions
} from './db';
import { FileManager, type File, type UnlockedFile } from './db/file';
import { FileAccessManager, type FileAccess } from './db/file-access';
import { FileContentManager } from './db/file-content';
import { FileDataManager } from './db/file-data';
import { FileSnapshotManager } from './db/file-snapshot';
import { UserManager, type UpdateUserOptions, type User } from './db/user';
import { UserKeyManager, type UnlockedUserKey } from './db/user-key';
import { UserSessionManager } from './db/user-session';
import { randomBytes } from './utils';

export async function random(size: number): Promise<Uint8Array> {
  const bytes = await randomBytes(size);

  return bytes;
}

export function echo<T extends any>(data: T): T {
  return data;
}

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

  const [users, userKeys, userSessions] = await getManagers(
    UserManager,
    UserKeyManager,
    UserSessionManager
  );
  const { userSessionId, userSessionKey } = authentication;

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

  const unlockedUserSessions = userSessions.unlock(userSession, userSessionKey);

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

export async function authenticate(
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
    userSessionKey: unlockedSession[UserSessionManager.KEY_UNLOCKED_KEY]
  };
}

export async function validateAuthentication(authentication: Authentication): Promise<boolean> {
  try {
    await requireAuthentication(authentication);
    return true;
  } catch {
    return false;
  }
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

  const [user] = await users.create(
    username,
    firstName,
    middleName,
    lastName,
    password,
    UserRole.SiteAdmin
  );

  return user;
}

export async function getUser(
  authentication: Authentication | null,
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
  authentication: Authentication | null,
  options?: QueryOptions<UserManager, User>
): Promise<User[]> {
  await requireAuthentication(authentication);

  const [users] = await getManagers(UserManager);

  return await users.query(options);
}

export async function createUser(
  authentication: Authentication | null,
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
  authentication: Authentication | null,
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

  const result = await users.update(user, newData);
  return result;
}

export async function suspendUser(
  authentication: Authentication | null,
  id: number
): Promise<User> {
  const unlockedUserKey = await requireAuthentication(authentication);
  await ensureUserRole(unlockedUserKey, UserRole.SiteAdmin);

  const [users] = await getManagers(UserManager);
  const user = await users.getById(id);
  if (user == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }

  return await users.suspend(user);
}

export async function createFile(
  authentication: Authentication | null,
  parentFolderId: number,
  name: string,
  content: Uint8Array
): Promise<File> {
  const unlockedUserKey = await requireAuthentication(authentication);
  const [files, fileContents, fileSnapshotManager, fileDataManager] = await getManagers(
    FileManager,
    FileContentManager,
    FileSnapshotManager,
    FileDataManager
  );
  const parentFolder = await files.getById(parentFolderId);

  if (parentFolder == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }
  let unlockedParentFolder: UnlockedFile & {
    type: FileType.Folder;
  };

  try {
    unlockedParentFolder = (await files.unlock(parentFolder, unlockedUserKey)) as UnlockedFile & {
      type: FileType.Folder;
    };
  } catch {
    ApiError.throw(ApiErrorType.Forbidden, 'Failed to unlock parent folder.');
  }

  const unlockedFile = await files.create(
    unlockedUserKey,
    unlockedParentFolder,
    name,
    FileType.File
  );
  const fileContent = await fileContents.getMain(unlockedFile);
  const fileSnapshot = await fileSnapshotManager.getMain(unlockedFile, fileContent);

  await fileDataManager.write(unlockedFile, fileContent, fileSnapshot, 0, content);

  return (await files.getById(unlockedFile[FileManager.KEY_ID]))! as File & {
    [FileManager.KEY_TYPE]: FileType.File;
  };
}

export async function createFolder(
  authentication: Authentication | null,
  parentFolderId: number,
  name: string
): Promise<File> {
  const unlockedUserKey = await requireAuthentication(authentication);
  const [files] = await getManagers(FileManager);
  const parentFolder = await files.getById(parentFolderId);

  if (parentFolder == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }

  let unlockedParentFolder: UnlockedFile & {
    type: FileType.Folder;
  };

  try {
    unlockedParentFolder = (await files.unlock(parentFolder, unlockedUserKey)) as UnlockedFile & {
      type: FileType.Folder;
    };
  } catch {
    ApiError.throw(ApiErrorType.Forbidden, 'Failed to unlock parent folder.');
  }

  const unlockedFolder = await files.create(
    unlockedUserKey,
    unlockedParentFolder,
    name,
    FileType.Folder
  );
  return (await files.getById(unlockedFolder[FileManager.KEY_ID]))! as File & {
    [FileManager.KEY_TYPE]: FileType.Folder;
  };
}

export async function scanFolder(
  authentication: Authentication | null,
  folderId: number
): Promise<File[]> {
  const unlockedUserKey = await requireAuthentication(authentication);
  const [files] = await getManagers(FileManager);
  const folder = await files.getById(folderId);

  if (folder == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }

  let unlockedFolder: UnlockedFile & {
    type: FileType.Folder;
  };

  try {
    unlockedFolder = (await files.unlock(folder, unlockedUserKey)) as UnlockedFile & {
      type: FileType.Folder;
    };
  } catch {
    ApiError.throw(ApiErrorType.Forbidden, 'Failed to unlock parent folder.');
  }

  return files.scanFolder(unlockedFolder);
}

export async function grantAccessToUser(
  authentication: Authentication | null,
  fileId: number,
  targetUserId: number,
  type: FileAccessLevel
): Promise<FileAccess> {
  const unlockedUserKey = await requireAuthentication(authentication);
  const [fileManager, fileAccessManager, userManager] = await getManagers(
    FileManager,
    FileAccessManager,
    UserManager
  );

  const file = await fileManager.getById(fileId);
  if (file == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }

  let unlockedFile: UnlockedFile;

  try {
    unlockedFile = await fileManager.unlock(file, unlockedUserKey, FileAccessLevel.Manage);
  } catch {
    ApiError.throw(ApiErrorType.Forbidden, 'Failed to unlock parent folder.');
  }

  const user = await userManager.getById(targetUserId);
  if (user == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }

  const fileAccess = await fileAccessManager.create(unlockedFile, user, type);
  return (await fileAccessManager.getById(fileAccess[FileAccessManager.KEY_ID]))!;
}

export async function listPathChain(
  authentication: Authentication | null,
  fileId: number
): Promise<File[]> {
  const unlockedUserKey = await requireAuthentication(authentication);
  const [fileManager] = await getManagers(FileManager, FileAccessManager);

  let file = await fileManager.getById(fileId);
  if (file == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }

  try {
    await fileManager.unlock(file, unlockedUserKey, FileAccessLevel.Manage);
  } catch {
    ApiError.throw(ApiErrorType.Forbidden, 'Failed to unlock parent folder.');
  }

  const files: File[] = [];
  while (file != null) {
    files.push(file);

    const parentFolderId = file[FileManager.KEY_PARENT_FILE_ID];
    if (parentFolderId == null) {
      break;
    }

    file = await fileManager.getById(parentFolderId);
  }

  return files;
}

export async function listFileAccess(
  authentication: Authentication | null,
  fileId: number
): Promise<FileAccess[]> {
  const unlockedUserKey = await requireAuthentication(authentication);
  const [files, fileAccessManager] = await getManagers(FileManager, FileAccessManager);
  const file = await files.getById(fileId);

  if (file == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }

  let unlockedFile: UnlockedFile;
  try {
    unlockedFile = await files.unlock(file, unlockedUserKey);
  } catch {
    ApiError.throw(ApiErrorType.Forbidden, 'Failed to unlock parent folder.');
  }

  let fileAccesses: FileAccess[];

  if (file[FileManager.KEY_OWNER_USER_ID] !== unlockedUserKey[UserKeyManager.KEY_USER_ID]) {
    fileAccesses = await fileAccessManager.query({
      where: [
        [FileAccessManager.KEY_FILE_ID, '=', unlockedFile.id],
        [FileAccessManager.KEY_USER_ID, '=', unlockedUserKey.userId]
      ],
      orderBy: [[FileAccessManager.KEY_ACCESS_LEVEL, true]]
    });
  } else {
    fileAccesses = await fileAccessManager.query({
      where: [[FileAccessManager.KEY_FILE_ID, '=', unlockedFile.id]],
      orderBy: [[FileAccessManager.KEY_ACCESS_LEVEL, true]]
    });
  }

  return fileAccesses;
}

export async function listSharedFiles(
  authentication: Authentication | null
): Promise<FileAccess[]> {
  const unlockedUserKey = await requireAuthentication(authentication);

  const [files, fileAccessManager] = await getManagers(FileManager, FileAccessManager);
  const fileAccesses = await fileAccessManager.query({
    where: [
      [FileAccessManager.KEY_USER_ID, '=', unlockedUserKey[UserKeyManager.KEY_USER_ID]],
      [FileAccessManager.KEY_ACCESS_LEVEL, '>=', FileAccessLevel.Read]
    ]
  });

  return fileAccesses;
}

export async function getFile(
  authentication: Authentication | null,
  fileId: number | null
): Promise<File> {
  const unlockedUserKey = await requireAuthentication(authentication);
  const [files] = await getManagers(FileManager);
  const file = fileId != null ? await files.getById(fileId) : await files.getRoot(unlockedUserKey);
  if (file == null) {
    ApiError.throw(ApiErrorType.NotFound);
  }

  try {
    await files.unlock(file, unlockedUserKey, FileAccessLevel.Read);
    return file;
  } catch {
    ApiError.throw(ApiErrorType.Forbidden, 'Failed to unlock parent folder.');
  }
}
