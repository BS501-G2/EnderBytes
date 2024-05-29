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

export async function createTest(test: string): Promise<TestData> {
  const database = await Database.getInstance();
  const testManager = database.getManager(TestManager);

  const result = await testManager.create(test);
  return result;
}

export async function getTest(id: number): Promise<TestData | null> {
  const database = await Database.getInstance();
  const testManager = database.getManager(TestManager);
  const result = await testManager.getById(id);

  return result;
}

export async function updateTest(id: number, test: string): Promise<TestData> {
  const database = await Database.getInstance();
  const testManager = database.getManager(TestManager);

  await testManager.update(id, { test });

  return <TestData>await testManager.getById(id);
}

export async function listTestVersion(id: number): Promise<TestData[]> {
  const database = await Database.getInstance();
  const testManager = database.getManager(TestManager);

  return await testManager.listVersions(id);
}
export async function deleteTest(id: number): Promise<void> {
  const database = await Database.getInstance();
  const testManager = database.getManager(TestManager);

  const testData = (await getTest(id))!;
  await testManager.delete(testData);
  // await testManager.purge(id);
}
