import { goto } from '$app/navigation';
import { getAndVerifyAuthentication, getAuthentication, getServerStatus, getUser } from '$lib/client/api-functions';
import { UserRole } from '$lib/shared/db';

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

  const user = (await getUser(authentication.userId))!;
  if (user.role < UserRole.SiteAdmin) {
    await goto('/', { replaceState: true });
    return;
  }
}
