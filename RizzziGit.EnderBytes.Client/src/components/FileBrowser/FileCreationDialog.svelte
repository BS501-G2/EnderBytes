<script lang="ts" context="module">
  export enum CreateNewFileType {
    File,
    Folder,
  }

  let types: CreateNewFileType[] = [
    CreateNewFileType.File,
    CreateNewFileType.Folder,
  ];
</script>

<script lang="ts">
  import type { Client } from "$lib/client/client";
  import Button, { ButtonClass } from "../Button.svelte";
  import Dialog, { DialogClass } from "../Dialog.svelte";
  import FileCreationTab from "./FileCreationDialog/FileCreationTab.svelte";

  export let client: Client;

  let hasUnsavedProgress = true;
  let current: CreateNewFileType = CreateNewFileType.File;

  let dismissConfirmationDialog = false;

  export let onDismiss: () => void;

  function cancel() {
    if (hasUnsavedProgress) {
      dismissConfirmationDialog = true;
      return;
    }

    onDismiss();
  }
</script>

<Dialog
  dialogClass={DialogClass.Normal}
  onDismiss={() => {
    cancel();
  }}
>
  <h2 slot="head" style="margin: 0px">Create New File</h2>
  <div slot="body" class="file-creation-dialog">
    <div class="creation-tab">
      {#each types as type}
        <Button
          buttonClass={current == type
            ? ButtonClass.PrimaryContainer
            : ButtonClass.Background}
          onClick={() => {
            current = type;
          }}
          >{(() => {
            switch (type) {
              case CreateNewFileType.File:
                return "Create a File";
              case CreateNewFileType.Folder:
                return "Create a Folder";
            }
          })()}</Button
        >
      {/each}
    </div>
    <div class="divider"></div>
    <div class="creation-panel">
      {#if current == CreateNewFileType.File}
        <FileCreationTab />
      {:else if current == CreateNewFileType.Folder}
        <p>Folder Page</p>
      {/if}
    </div>
  </div>
  <svelte:fragment slot="actions">
    <Button buttonClass={ButtonClass.Primary} onClick={() => {}}>Create</Button>
    <Button
      buttonClass={ButtonClass.Background}
      onClick={async () => {
        cancel();
      }}>Cancel</Button
    >
  </svelte:fragment>
</Dialog>

{#if dismissConfirmationDialog}
  <Dialog
    dialogClass={DialogClass.Normal}
    onDismiss={() => (dismissConfirmationDialog = false)}
  >
    <h2 slot="head" style="margin: 0px;">Unsaved Progress</h2>
    <p slot="body" style="margin: 0px;">
      All unsaved progress will be lost. Are you sure to close the file creation
      dialog?
    </p>
    <svelte:fragment slot="actions">
      <Button onClick={() => onDismiss()}>Yes</Button>
      <Button
        onClick={() => {
          dismissConfirmationDialog = false;
        }}>No</Button
      >
    </svelte:fragment>
  </Dialog>
{/if}

<style lang="scss">
  div.file-creation-dialog {
    max-width: 100%;

    width: 854px;
    height: min(480px, calc(100vh - 256px));

    display: flex;

    overflow-x: auto;

    > div.creation-tab {
      width: 256px;
      padding: 8px;
      box-sizing: border-box;

      display: flex;
      flex-direction: column;
      gap: 8px;

      overflow-y: auto;
      min-height: 0px;
    }

    > div.divider {
      width: 1px;

      background-color: var(--primary);
    }

    > div.creation-panel {
      flex-grow: 1;
    }
  }
</style>
