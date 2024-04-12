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
  import Button, { ButtonClass } from "../../Widgets/Button.svelte";
  import Dialog, { DialogClass } from "../../Widgets/Dialog.svelte";
  import FileCreationTab from "./FileCreationTab.svelte";
  import FolderCreationTab from "./FolderCreationTab.svelte";

  export let folderId: number | null;
  export let client: Client;

  let hasUnsavedProgress = true;
  let current: CreateNewFileType = CreateNewFileType.File;
  let onSubmit: () => Promise<number> | number;
  let submitPromise: Promise<number> | null = null;

  let dismissConfirmationDialog = false;

  export let onDismiss: () => void;

  function cancel(ignoreUnsaved: boolean = false) {
    if (!ignoreUnsaved && hasUnsavedProgress) {
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
        <FileCreationTab {client} {folderId} bind:onSubmit />
      {:else if current == CreateNewFileType.Folder}
        <FolderCreationTab {client} {folderId} bind:onSubmit />
      {/if}
    </div>
  </div>
  <svelte:fragment slot="actions">
    <Button
      enabled={submitPromise == null}
      buttonClass={ButtonClass.Primary}
      onClick={async () => {
        try {
          await (submitPromise = (async () => await onSubmit())());

          onDismiss();
        } finally {
          submitPromise = null;
        }
      }}
    >
      Create
    </Button>
    <Button
      enabled={submitPromise == null}
      buttonClass={ButtonClass.Background}
      onClick={cancel}
    >
      Cancel
    </Button>
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
      <Button onClick={onDismiss}>Yes</Button>
      <Button
        onClick={() => {
          dismissConfirmationDialog = false;
        }}
      >
        No
      </Button>
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
      width: 172px;
      padding: 0px 8px 0px 8px;
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

      display: flex;

      flex-direction: column;

      padding: 0px 16px 0px 16px;
    }
  }
</style>
