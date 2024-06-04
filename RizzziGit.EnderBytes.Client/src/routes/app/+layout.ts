import { goto } from '$app/navigation';
import {
  getAndVerifyAuthentication,
  getAuthentication,
  getServerStatus
} from '$lib/client/api-functions';

export async function load(): Promise<void> {
  const status = await getServerStatus();

  if (status.setupRequired) {
    await goto('/setup', { replaceState: true });
    return;
  }

  const authentication = await getAndVerifyAuthentication();

  if (authentication == null) {
    await goto('/login', { replaceState: true });
    return;
  }
}
