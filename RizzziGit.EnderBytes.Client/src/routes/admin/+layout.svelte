<script lang="ts">
  import { Awaiter } from '@rizzzi/svelte-commons';
  import { type Snippet } from 'svelte';
  import { goto } from '$app/navigation';
  import { clientSideInvoke } from '$lib/client/api'

  const { children }: { children: Snippet } = $props();
</script>

<Awaiter
  callback={async () => {
    const status = await clientSideInvoke('getServerStatus');

    if (status.setupRequired) {
      goto('/admin/setup', { replaceState: true });
    }
  }}
>
  {#snippet success()}
    {@render children()}
  {/snippet}
</Awaiter>
