<script lang="ts" context="module">
  export enum DialogClass {
    Normal = "normal",
    Warning = "warning",
    Error = "error",
  }
</script>

<script lang="ts">
  import { RootState } from "$lib/states/root-state";
  import { XIcon } from "svelte-feather-icons";

  import ResponsiveLayout from "../Bindings/ResponsiveLayout.svelte";
  import Button, { ButtonClass } from "./Button.svelte";
  import Modal from "./Modal.svelte";

  const rootState = RootState.state;

  export let onDismiss: () => void;
  export let dialogClass: DialogClass = DialogClass.Normal;
</script>

<Modal {onDismiss}>
  <div class="dialog {dialogClass} {$rootState.isMobile ? 'mobile' : ''}">
    {#if $$slots.head}
      {#if $rootState.isDesktop}
        <div class="head">
          <slot name="head" />
        </div>
      {:else if $rootState.isMobile}
        <div class="head-mobile">
          <div class="head-element">
            <slot name="head" />
          </div>
          <Button
            buttonClass={ButtonClass.Background}
            onClick={() => onDismiss()}><XIcon /></Button
          >
        </div>
      {/if}
    {/if}
    {#if $$slots.body}
      <div
        class="body"
        style="{$$slots.head ? 'margin-top: 16px;' : ''} {$$slots.actions
          ? 'margin-bottom: 16px'
          : ''}"
      >
        <slot name="body" />
      </div>
    {/if}
    {#if $$slots.actions}
      <div class="actions">
        <slot name="actions" />
      </div>
    {/if}
  </div>
</Modal>

<style lang="scss">
  div.dialog {
    max-width: calc(100vw - 32px);
    max-height: calc(100vh - 32px);
    min-height: 128px;

    box-sizing: border-box;

    padding: 32px;
    border: solid 1px var(--primary);
    border-radius: 8px;
    box-shadow: gray 0px 0px 8px;

    display: flex;
    flex-direction: column;
    justify-content: space-between;

    gap: 8px;

    div.head-mobile {
      display: flex;

      > div.head-element {
        flex-grow: 1;
      }
    }

    > div.head {
      text-align: left;
    }

    > div.body {
      flex-grow: 1;
      text-align: left;

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

  div.dialog.mobile {
    width: 100vw;
    height: 100vh;

    max-width: 100vw;
    max-height: 100vh;

    padding: 16px;

    border: none;
    border-radius: 0px;
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
