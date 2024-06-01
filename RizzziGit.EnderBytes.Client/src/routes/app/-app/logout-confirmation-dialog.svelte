<script lang="ts" context="module">
  import { writable, type Writable } from 'svelte/store';

  export const enabled: Writable<boolean> = writable(false);
</script>

<script lang="ts">
  import { Button, Dialog, DialogClass, ButtonClass } from '@rizzzi/svelte-commons';
  import { clearAuthentication } from '$lib/client/api-functions';
  import { goto } from '$app/navigation';
</script>

{#if $enabled}
  <Dialog dialogClass={DialogClass.Normal} onDismiss={() => ($enabled = false)}>
    {#snippet actions()}
      <Button
        onClick={async () => {
          $enabled = false;
          clearAuthentication();
          await goto('/login', { replaceState: true });
        }}
        buttonClass={ButtonClass.Primary}
      >
        <p class="label">OK</p>
      </Button>
      <Button
        onClick={() => {
          $enabled = false;
        }}
        buttonClass={ButtonClass.Background}
      >
        <p class="label">Cancel</p>
      </Button>
    {/snippet}
    {#snippet head()}
      <h2 style="margin: 0px;">Account Logout</h2>
    {/snippet}
    {#snippet body()}
      <span>This will log you out from the dashboard.</span>
    {/snippet}
  </Dialog>
{/if}

<style lang="scss">
  p.label {
    margin: 8px;
  }
</style>
