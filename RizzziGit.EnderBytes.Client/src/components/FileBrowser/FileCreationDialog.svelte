<script lang="ts">
  import type { Client } from "$lib/client/client";
  import { ButtonClass } from "../Button.svelte";
  import Dialog, { DialogClass } from "../Dialog.svelte";

  export let client: Client;

  let busy: boolean = false;

  export let onDismiss: () => void;
</script>

<Dialog
  dialogClass={DialogClass.Normal}
  onDismiss={() => {
    if (busy) {
      return;
    }

    onDismiss();
  }}
  buttons={[
    {
      label: "Create",
      buttonClass: ButtonClass.Primary,
      enabled: !busy,
      onClick: () => {},
    },
    {
      label: "Cancel",
      buttonClass: ButtonClass.Background,
      enabled: !busy,
      onClick: () => {
        onDismiss();
      },
    },
  ]}
>
  <b slot="head">Create new File</b>
  <div slot="body" class="file-creation-dialog"></div>
</Dialog>

<style lang="scss">
</style>
