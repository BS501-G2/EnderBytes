import type { Authentication, AuthenticationRequest } from '$lib/shared/api';
import type { UpdateUserOptions, User, UserManager } from '$lib/server/db/user';
import type { UnlockedUserKey } from '$lib/server/db/user-key';
import type { QueryOptions } from '$lib/server/db';

import { persisted } from 'svelte-persisted-store';
import { derived, get, type Writable } from 'svelte/store';
import { clientSideInvoke } from './api';
import { UserKeyType, UserRole } from '$lib/shared/db';
import { BSON } from 'bson';
import { bytesToBase64, base64ToBytes } from 'byte-base64';

const authentication: Writable<Authentication | null> = persisted('authentication', null, {
  serializer: {
    stringify: (value) => bytesToBase64(BSON.serialize({ data: value })),
    parse: (json) => BSON.deserialize(base64ToBytes(json)).data
  }
});

export async function authenticateByPassword(username: string, password: string): Promise<Authentication> {
  const result = await clientSideInvoke(
    'autenticate',
    username,
    UserKeyType.Password,
    new TextEncoder().encode(password)
  );

  authentication.set(result);
  return result
}

export function getAuthentication(): Authentication | null {
  return get(authentication);
}

export async function getAndVerifyAuthentication(): Promise<Authentication | null> {
  const value = requestAuthentication();
  if (value == null) {
    return null;
  }

  if (!(await clientSideInvoke('validateAuthentication', value))) {
    return null;
  }

  return getAuthentication();
}

export function clearAuthentication(): void {
  authentication.set(null);
}

function requestAuthentication(): AuthenticationRequest | null {
  const value = get(authentication);
  if (value == null) {
    return null;
  }

  return {
    userSessionId: value.userSessionId,
    userSessionAuthTag: value.userSessionAuthTag
  };
}

export async function getServerStatus() {
  return await clientSideInvoke('getServerStatus');
}

export async function createAdminUser(
  username: string,
  firstName: string,
  middleName: string | null,
  lastName: string,
  password: string
): Promise<User> {
  return await clientSideInvoke(
    'createAdminUser',
    username,
    firstName,
    middleName,
    lastName,
    password
  );
}

export async function listUsers(options?: QueryOptions<UserManager, User>): Promise<User[]> {
  return await clientSideInvoke('listUsers', requestAuthentication(), options);
}

export async function getUser(id: number | string): Promise<User | null> {
  return await clientSideInvoke('getUser', requestAuthentication(), id)
}

export async function createUser(
  username: string,
  firstName: string,
  middleName: string | null,
  lastName: string,
  role: UserRole
): Promise<[user: User, unlockedUserKey: UnlockedUserKey, password: string]> {
  return await clientSideInvoke(
    'createUser',
    requestAuthentication(),
    username,
    firstName,
    middleName,
    lastName,
    role
  );
}

export async function updateUser(id: number, newData: UpdateUserOptions) {
  return await clientSideInvoke('updateUser', requestAuthentication(), id, newData);
}

const readonlyAuthentication = derived(authentication, (value) => {
  if (value == null) {
    return null;
  }

  return value;
});

export { readonlyAuthentication as authentication };
