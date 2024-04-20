<script lang="ts">
  import {
    ScissorsIcon,
    CopyIcon,
    TrashIcon,
    ShareIcon,
    UsersIcon,
    RefreshCwIcon,
    FolderPlusIcon,
    UploadIcon,
  } from "svelte-feather-icons";
  import { type AwaiterResetFunction } from "../../Bindings/Awaiter.svelte";
  import Button, { ButtonClass } from "../../Widgets/Button.svelte";
  import { enabled as fileCreationDialog } from "../FileCreationDialog.svelte";
  import { enabled as folderCreationDialog } from "../FolderCreationDialog.svelte";
  import type {
    FileBrowserInformation,
    FileBrowserSelection,
  } from "../../FileBrowser.svelte";

  export let selection: FileBrowserSelection;
  export let reset: AwaiterResetFunction;
  export let info: FileBrowserInformation | null;
</script>

<div class="controls">
  {#if info == null}
    <div class="loading"></div>
  {:else}
    {@const file = info.current}

    <Button
      buttonClass={ButtonClass.PrimaryContainer}
      outline={false}
      onClick={() => ($fileCreationDialog = true)}
    >
      <UploadIcon />
      <p>Upload</p>
    </Button>
    <Button
      buttonClass={ButtonClass.PrimaryContainer}
      outline={false}
      onClick={() => ($folderCreationDialog = true)}
    >
      <FolderPlusIcon />
      <p>New Folder</p>
    </Button>
    <div class="divider" />
    {#if file == null || file?.type === 1}
      <Button
        buttonClass={ButtonClass.PrimaryContainer}
        outline={false}
        onClick={reset}
      >
        <RefreshCwIcon />
        <p>Refresh</p>
      </Button>
    {/if}
    {#if $selection.length !== 0}
      <Button
        buttonClass={ButtonClass.PrimaryContainer}
        outline={false}
        onClick={() => {}}
      >
        <ScissorsIcon />
        <p>Move To</p>
      </Button>
      <Button
        buttonClass={ButtonClass.PrimaryContainer}
        outline={false}
        onClick={() => {}}
      >
        <CopyIcon />
        <p>Copy To</p>
      </Button>
    {/if}
    {#if $selection.length === 1}
      <Button
        buttonClass={ButtonClass.PrimaryContainer}
        outline={false}
        onClick={() => {}}
      >
        <ShareIcon />
        <p>Share</p>
      </Button>
      <Button
        buttonClass={ButtonClass.PrimaryContainer}
        outline={false}
        onClick={() => {}}
      >
        <UsersIcon />
        <p>Manage Access</p>
      </Button>
    {/if}
    {#if $selection.length >= 1}
      <Button
        buttonClass={ButtonClass.PrimaryContainer}
        outline={false}
        onClick={() => {}}
      >
        <TrashIcon />
        <p>Move to Trash</p>
      </Button>
    {/if}
  {/if}
</div>

<style lang="scss">
  div.controls {
    background-color: var(--primaryContainer);

    display: flex;
    flex-direction: row;
    gap: 8px;
    align-items: center;

    max-height: 64px;
    min-height: 64px;

    padding: 16px;
    box-sizing: border-box;

    > div.divider {
      min-width: 1px;
      max-width: 1px;
      height: 100%;
      background-color: var(--onBackground);
      margin: 4px 0px 4px 0px;
    }
  }
</style>
