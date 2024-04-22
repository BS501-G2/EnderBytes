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
    BarChart2Icon,
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

  $: enabled = info != null;
  $: file = info?.current;

  const buttonClass = ButtonClass.BackgroundVariant;
  const outline = false;
  const size = "18em";
</script>

<div class="controls">
  <div class="section left-section">
    <Button
      {buttonClass}
      {outline}
      {enabled}
      onClick={() => ($fileCreationDialog = true)}
    >
      <div class="button">
        <UploadIcon {size} />
        <p class="button-label">Upload</p>
      </div>
    </Button>
    <Button
      {buttonClass}
      {outline}
      {enabled}
      onClick={() => ($folderCreationDialog = true)}
    >
      <div class="button">
        <FolderPlusIcon {size} />
        <p class="button-label">New Folder</p>
      </div>
    </Button>
    <div class="divider" />
    {#if $selection.length !== 0}
      <Button {buttonClass} {outline} onClick={() => {}} {enabled}>
        <div class="button">
          <ScissorsIcon {size} />
          <p class="button-label">Move To</p>
        </div>
      </Button>
      <Button {buttonClass} {outline} onClick={() => {}} {enabled}>
        <div class="button">
          <CopyIcon {size} />
          <p class="button-label">Copy To</p>
        </div>
      </Button>
    {/if}
    {#if $selection.length === 1}
      <Button {buttonClass} {outline} onClick={() => {}} {enabled}>
        <div class="button">
          <ShareIcon {size} />
          <p class="button-label">Share</p>
        </div>
      </Button>
      <Button {buttonClass} {outline} onClick={() => {}} {enabled}>
        <div class="button">
          <UsersIcon {size} />
          <p class="button-label">Manage Access</p>
        </div>
      </Button>
    {/if}
    {#if $selection.length >= 1}
      <Button {buttonClass} {outline} onClick={() => {}} {enabled}>
        <div class="button">
          <TrashIcon {size} />
          <p class="button-label">Move to Trash</p>
        </div>
      </Button>
    {/if}
  </div>
  <div class="divider" />
  <div class="section right-section">
    {#if file == null || file?.type === 1}
      <Button {buttonClass} {outline} onClick={reset} {enabled}>
        <div class="button">
          <RefreshCwIcon {size} />
          <p class="button-label">Refresh</p>
        </div>
      </Button>
    {/if}
    <Button {buttonClass} {outline} onClick={async () => {
      await new Promise((resolve) => setTimeout(resolve, 1000))
      console.log('asd')
    }} {enabled}>
      <div class="button">
        <BarChart2Icon {size} />
        <p class="button-label">Sort</p>
      </div>
    </Button>
  </div>
</div>

<style lang="scss">
  div.controls {
    background-color: var(--backgroundVariant);
    border-radius: 8px;

    user-select: none;

    display: flex;
    flex-direction: row;
    // gap: 8px;

    box-sizing: border-box;

    overflow-x: auto;
    overflow-y: hidden;

    div.divider {
      min-width: 1px;
      max-width: 1px;

      background-color: var(--onBackground);
      margin: 8px 0px 8px 0px;
    }

    div.button {
      display: flex;
      flex-direction: row;
      align-items: center;
      gap: 8px;

      min-width: max-content;
    }

    div.section {
      display: flex;
      flex-direction: row;
      // gap: 8px;
    }

    div.left-section {
      flex-grow: 1;
    }
  }
</style>
