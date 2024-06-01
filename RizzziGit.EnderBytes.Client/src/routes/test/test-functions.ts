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
    'Get Server Status',
    async () => {
      return await getServerStatus();
    }
  ],
  [
    'Get Admin User Credentials',
    async () => {
      return {
        username: adminUser,
        password: adminPassword
      };
    }
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
  [
    'List Users',
    async ()=> {
      const authentication = getAuthentication()
      const result = await listUsers()

      return result
    }
  ],
  [
    'Echo',
    async (log) => {
      const bytes = new Uint8Array(1024 * 1024 * 8)
      log(`Sending random ${bytes.length} bytes.`)

      for (let i = 0; i < bytes.length; i++) {
        bytes[i] = Math.floor(Math.random() * 256)
      }

      const result: Uint8Array = await clientSideInvoke('echo', bytes) as Uint8Array
      log(result)
    }
  ],
  [
    'Random Bytes',
    async (log) => {
      const bytes = await clientSideInvoke('random', 1024 * 1024 * 8)

      log(bytes)
    }
  ]
];
