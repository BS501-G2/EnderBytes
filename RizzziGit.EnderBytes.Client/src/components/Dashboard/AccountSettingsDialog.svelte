<script lang="ts">
  import Dialog from "../Widgets/Dialog.svelte";

  export let onDismiss: () => void;
</script>

<Dialog {onDismiss}>
  <h2 slot="head" style="margin: 0px;">Account settings</h2>
  <div class="account-settings" slot="body">

    // TODO: Add account settings

  </div>
</Dialog>
