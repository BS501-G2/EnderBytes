import { BSON } from 'bson';

import * as ApiFunctions from './api-functions';

export interface ServerStatus {
  requiresAdminSetup: boolean;
}

export type Map = typeof ApiFunctions;

export function functionExists<T extends keyof Map>(name: T): boolean {
  return (name in ApiFunctions);
}

export async function invoke<T extends keyof Map>(
  name: T,
  ...args: Parameters<Map[T]>
): Promise<Awaited<ReturnType<Map[T]>>> {
  const func = ApiFunctions[name];
  const result = await (func as any).apply(undefined, args);

  return <never>result;
}
