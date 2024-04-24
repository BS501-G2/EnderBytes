<script lang="ts" context="module">
  import { writable, type Writable } from "svelte/store";

  export const enabled: Writable<boolean> = writable(false);
</script>

<script>
  import Client from "../Bindings/Client.svelte";
  import Button, { ButtonClass } from "../Widgets/Button.svelte";
  import Dialog, { DialogClass } from "../Widgets/Dialog.svelte";
</script>

{#if $enabled}
  <Dialog dialogClass={DialogClass.Normal} onDismiss={() => ($enabled = false)}>
    <svelte:fragment slot="actions">
      <Client let:apiFetch>
        <Button
          onClick={() => {
            $enabled = false;
            return apiFetch("/auth/logout", "POST");
          }}
          buttonClass={ButtonClass.Primary}
        >
          OK
        </Button>
        <Button
          onClick={() => ($enabled = false)}
          buttonClass={ButtonClass.Background}
        >
          Cancel
        </Button>
      </Client>
    </svelte:fragment>
    <h2 slot="head" style="margin: 0px;">Account Logout</h2>
    <span slot="body">This will log you out from the dashboard.</span>
  </Dialog>
{/if}
