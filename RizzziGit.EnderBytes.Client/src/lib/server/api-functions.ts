import { Database } from './db';
import { TestManager, type TestData } from './db/test';
import { UserManager, type User } from './db/user';
import { UserRoleManager, UserRoleType, type UserRole } from './db/user-role';

export function echo<T>(data: T): T {
  return data;
}

export interface ServerStatus {
  setupRequired: boolean;
}

export async function getServerStatus(): Promise<ServerStatus> {
  const database = await Database.getInstance();
  const userRoleManager = database.getManager(UserRoleManager);

  return {
    setupRequired: (await userRoleManager.count()) === 0
  };
}

export interface CreateAdminUserRequest {
  username: string;
  firstName: string;
  middleName: string | null;
  lastName: string;
  password: string;
}

export type CreateAdminUserResponse = [user: User, userRole: UserRole];

export async function createAdminUser(
  request: CreateAdminUserRequest
): Promise<CreateAdminUserResponse> {
  const database = await Database.getInstance();
  const status = await getServerStatus();

  if (!status.setupRequired) {
    throw new Error('Admin user already exists');
  }

  const userManager = database.getManager(UserManager);
  const userRoleManager = database.getManager(UserRoleManager);

  const [user] = await userManager.create(
    request.username,
    request.firstName,
    request.middleName,
    request.lastName,
    request.password
  );

  const userRole = await userRoleManager.create(user, UserRoleType.SiteAdmin);
  return [user, userRole];
}

export async function testFunction(test: string): Promise<TestData> {
  const database = await Database.getInstance();
  const testManager = database.getManager(TestManager);

  return await testManager.create(test);
}

export async function getTest(id: number): Promise<TestData | null> {
  const database = await Database.getInstance();
  const testManager = database.getManager(TestManager);
  return await testManager.getById(id);
}
