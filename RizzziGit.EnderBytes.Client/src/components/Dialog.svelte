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
  export let buttons: DialogButton[];
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
      {#each buttons as button}
        <div class="button-entry">
          <Button
            onClick={button.onClick}
            buttonClass={button.buttonClass ?? ButtonClass.Primary}
            label={button.label}
            enabled={button.enabled ?? true}
          />
        </div>
      {/each}
    </div>
  </div>
</Modal>

<style lang="scss">
  div.dialog {
    padding: 16px;
    border-radius: 8px;
    box-shadow: gray 2px 2px 8px;

    display: flex;
    flex-direction: column;

    > div.head {
      text-align: left;
    }

    > div.body {
      text-align: left;

      margin: 16px 0px 16px 0px;
      box-sizing: border-box;
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
