import { goto } from '$app/navigation';
import { getAndVerifyAuthentication, getAuthentication, getServerStatus } from '$lib/client/api-functions';

export async function load(): Promise<void> {
  const serverStatus = await getServerStatus()

  if (serverStatus.setupRequired) {
    return await goto('/setup', { replaceState: true });
  }

  const { searchParams } = new URL(window.location.href);

  const authentication = await getAndVerifyAuthentication();

  if (authentication != null) {
    return await goto(searchParams.get('return') ?? '/app', { replaceState: true });
  }
}
