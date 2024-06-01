import { goto } from '$app/navigation';
import { getAuthentication } from '$lib/client/api-functions';

export async function load(): Promise<void> {
  const { searchParams } = new URL(window.location.href);

  const authentication = getAuthentication();
  if (authentication != null) {
    return await goto(searchParams.get('return') ?? '/app', { replaceState: true });
  }
}
