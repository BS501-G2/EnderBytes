<script lang="ts">
  import { ArrowLeftIcon } from "svelte-feather-icons";

  export let onDismiss: () => void;
</script>

<div class="tab">
  <div class="header">
    <button class="back" on:click={onDismiss}><ArrowLeftIcon /></button>
    <slot name="head" />
  </div>
  <div
    style="min-height: 1px;  max-height: 1px; background-color: var(--primary);"
  />
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
      display: flex;
      flex-direction: row;
      align-items: center;
      gap: 8px;

      padding: 8px 0px 8px 0px;

      > button.back {
        background-color: unset;
        color: var(--primary);
        border: unset;

        cursor: pointer;

        display: flex;
        align-items: center;
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
