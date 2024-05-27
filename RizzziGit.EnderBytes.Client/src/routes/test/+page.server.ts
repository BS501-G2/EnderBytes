import { decryptSymmetric, encryptSymmetric, randomBytes } from '$lib/server/db/user-key';

export async function load() {
  const key = await randomBytes(32);
  const iv = await randomBytes(16);

  const test = Buffer.from('test', 'utf-8');
  const [authTag, encryptedTest] = encryptSymmetric(key, iv, test);

  const decrypted = decryptSymmetric(key, iv, encryptedTest, authTag);

  return { asdasd: decrypted.toString('utf-8') };
}
