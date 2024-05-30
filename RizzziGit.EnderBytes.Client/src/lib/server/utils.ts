import * as Crypto from 'crypto';

export type KeyPair = [privateKey: Uint8Array, publicKey: Uint8Array];

export const generateKeyPair = (): Promise<KeyPair> =>
  new Promise<KeyPair>((resolve, reject) => {
    Crypto.generateKeyPair(
      'rsa',
      {
        modulusLength: 2048,
        publicKeyEncoding: {
          type: 'spki',
          format: 'pem'
        },
        privateKeyEncoding: {
          type: 'pkcs8',
          format: 'pem'
        }
      },
      function (error: Error | null, publicKey: string, privateKey: string) {
        if (error) {
          reject(error);
        } else {
          resolve([Buffer.from(privateKey, 'utf-8'), Buffer.from(publicKey, 'utf-8')]);
        }
      }
    );
  });

export const hashPayload = (payload: Uint8Array, salt: Uint8Array): Promise<Uint8Array> =>
  new Promise((resolve, reject) => {
    Crypto.scrypt(payload, salt, 32, (error: Error | null, hash: Uint8Array) => {
      if (error) {
        reject(error);
      } else {
        resolve(hash);
      }
    });
  });

export const randomBytes = (length: number): Promise<Uint8Array> =>
  new Promise((resolve, reject) => {
    Crypto.randomBytes(length, (error: Error | null, buffer: Uint8Array) => {
      if (error) {
        reject(error);
      } else {
        resolve(buffer);
      }
    });
  });

export const encryptSymmetric = (
  key: Uint8Array,
  iv: Uint8Array,
  buffer: Uint8Array
): [authTag: Uint8Array, output: Uint8Array] => {
  const cipher = Crypto.createCipheriv('aes-256-gcm', key, iv);
  const output = Buffer.concat([cipher.update(buffer), cipher.final()]);
  const authTag = cipher.getAuthTag();

  return [authTag, output];
};

export const decryptSymmetric = (
  key: Uint8Array,
  iv: Uint8Array,
  buffer: Uint8Array,
  authTag: Uint8Array
): Uint8Array => {
  const decipher = Crypto.createDecipheriv('aes-256-gcm', key, iv);
  decipher.setAuthTag(authTag);

  return Buffer.concat([decipher.update(buffer), decipher.final()]);
};

export const decryptAsymmetric = (privateKey: Uint8Array, buffer: Uint8Array): Uint8Array =>
  Crypto.privateDecrypt(Buffer.from(privateKey), buffer);

export const encryptAsymmetric = (publicKey: Uint8Array, buffer: Uint8Array): Uint8Array =>
  Crypto.publicEncrypt(Buffer.from(publicKey), buffer);
