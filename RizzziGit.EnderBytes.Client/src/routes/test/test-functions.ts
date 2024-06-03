import { clientSideInvoke } from '$lib/client/api';
import {
  authenticateByPassword,
  createAdminUser,
  getAuthentication,
  getServerStatus,
  listUsers,
  updateUser
} from '$lib/client/api-functions';

type TestFunctions = [string, (log: (data: any) => void) => any | Promise<any>][];

const adminUser = 'testuser';
const adminPassword = 'testuser123;';
const adminFirstName = 'Hugh';
const adminMiddleName = 'G';
const adminLastName = 'Rection';

export const testFunctions: TestFunctions = [
  ['Hello', () => 'hello'],
  ['World', () => 'world'],
  [
    'Echo',
    async (log) => {
      const bytes = new Uint8Array(1024 * 1024 * 8);
      log(`Sending random ${bytes.length} bytes.`);

      for (let i = 0; i < bytes.length; i++) {
        bytes[i] = Math.floor(Math.random() * 256);
      }

      log(await clientSideInvoke('echo', bytes));
    }
  ],
  ['Random Bytes', async (log) => log(await clientSideInvoke('random', 1024 * 1024 * 8))],
  ['Get Server Status', () => getServerStatus()],
  [
    'Get Admin User Credentials',
    () => ({
      username: adminUser,
      password: adminPassword
    })
  ],
  [
    'Register Admin User',
    async () => {
      const user = await createAdminUser(
        adminUser,
        adminFirstName,
        adminMiddleName,
        adminLastName,
        adminPassword
      );

      return user;
    }
  ],
  [
    'Login As Admin',
    async () => {
      const result = await authenticateByPassword(adminUser, adminPassword);

      return result;
    }
  ],
  [
    'Update User Name',
    async () => {
      const authentication = getAuthentication();

      return await updateUser(authentication!.userId, { firstName: 'Test' + Date.now() });
    }
  ],
  ['List Users', () => listUsers()]
];
