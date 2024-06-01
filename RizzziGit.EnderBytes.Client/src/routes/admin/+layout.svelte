<script lang="ts">
  import { type Snippet } from 'svelte';
  import { type Writable, writable } from 'svelte/store';
  import { authentication } from '$lib/client/api-functions';
  import { Dialog } from '@rizzzi/svelte-commons';

  const { children }: { children: Snippet } = $props();

  const username: Writable<string> = writable('');
</script>

{#if $authentication != null}
  {@render children()}
{:else}
  <Dialog onDismiss={() => {}}>
    {#snippet head()}
      <h2>Login</h2>
    {/snippet}

    {#snippet body()}

    {/snippet}

    {#snippet actions()}{/snippet}
  </Dialog>
{/if}
