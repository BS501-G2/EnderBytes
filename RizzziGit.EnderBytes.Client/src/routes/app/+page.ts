import { goto } from '$app/navigation';

export async function load() {
  await goto('/app/feed', { replaceState: true });
}
