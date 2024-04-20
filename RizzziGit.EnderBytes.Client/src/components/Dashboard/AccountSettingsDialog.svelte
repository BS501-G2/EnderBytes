<script lang="ts" context="module">
  import { writable, type Writable } from "svelte/store";

  export const enabled: Writable<boolean> = writable(false);
</script>

<script lang="ts">
  import Dialog from "../Widgets/Dialog.svelte";

  // export let targetElement: HTMLElement | null = null;
</script>

{#if $enabled}
  <Dialog onDismiss={() => $enabled = false}>
    <h2 slot="head" style="margin: 0px;">Account settings</h2>
    <div class="account-settings" slot="body">
      // TODO: Add account settings
    </div>
  </Dialog>
{/if}
