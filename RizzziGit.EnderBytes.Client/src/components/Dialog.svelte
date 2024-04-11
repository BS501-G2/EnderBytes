<script lang="ts" context="module">
  export enum DialogClass {
    Normal = "normal",
    Warning = "warning",
    Error = "error",
  }

  export interface DialogButton {
    label: string;
    buttonClass?: ButtonClass;
    enabled?: boolean;
    onClick: () => void | Promise<void>;
  }
</script>

<script lang="ts">
  import Button, { ButtonClass } from "./Button.svelte";
  import Modal from "./Modal.svelte";

  export let onDismiss: () => void;
  export let dialogClass: DialogClass;
</script>

<Modal {onDismiss}>
  <div class="dialog {dialogClass}">
    <div class="head">
      <slot name="head" />
    </div>
    <div class="body">
      <slot name="body" />
    </div>
    <div class="actions">
      <slot name="actions" />
    </div>
  </div>
</Modal>

<style lang="scss">
  div.dialog {
    max-width: calc(100vw - 32px);
    max-height: calc(100vh - 32px);
    min-height: 128px;

    box-sizing: border-box;

    padding: 16px;
    border: solid 1px var(--primary);
    border-radius: 8px;
    box-shadow: gray 0px 0px 8px;

    display: flex;
    flex-direction: column;

    gap: 8px;

    > div.head {
      text-align: left;

      > h2 {
        margin: 0px;
      }
    }

    > div.body {
      text-align: left;

      margin: 16px 0px 16px 0px;
      box-sizing: border-box;

      overflow-y: auto;
      overflow-x: hidden;
    }

    > div.actions {
      display: flex;

      gap: 8px;
      justify-content: flex-end;
    }
  }

  div.dialog.normal {
    background-color: var(--background);
    color: var(--onBackground);
  }

  div.dialog.warning {
    background-color: #ffffa0;
    color: var(--onBackground);
  }

  div.dialog.error {
    background-color: #ffa0a0;
    color: var(--onBackground);
  }
</style>
