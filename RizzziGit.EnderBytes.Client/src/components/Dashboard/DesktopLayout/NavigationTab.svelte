<script lang="ts">
  import { ArrowLeftIcon } from "svelte-feather-icons";

  export let onDismiss: () => void;
</script>

<div class="tab">
  <div class="header">
    <button class="back" on:click={onDismiss}><ArrowLeftIcon /></button>
    <slot name="head" />
  </div>
  <div class="body">
    <slot name="body" />
  </div>
</div>

<style lang="scss">
  div.tab {
    display: flex;
    flex-direction: column;
    flex-grow: 1;

    min-height: 0px;

    div.header {
      border-bottom: 1px solid var(--primary);

      display: flex;
      flex-direction: row;
      align-items: center;
      gap: 8px;

      > button.back {
        background-color: unset;
        color: var(--primary);
        border: unset;

        cursor: pointer;
      }
    }

    div.body {
      display: flex;

      flex-direction: column;

      flex-grow: 1;

      min-height: 0px;
      overflow-y: auto;
    }
  }
</style>
