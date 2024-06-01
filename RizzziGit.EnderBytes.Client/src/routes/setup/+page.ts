import { goto } from '$app/navigation';
import { getServerStatus } from '$lib/client/api-functions';

export async function load() {
  const status = await getServerStatus();

  if (!status.setupRequired) {
    return await goto('/', { replaceState: true });
  }
}
