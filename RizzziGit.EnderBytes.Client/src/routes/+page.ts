import { goto } from "$app/navigation";

export async function load() {
  await goto('/landing', { replaceState: true });
}
