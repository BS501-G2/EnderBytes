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
  import Awaiter from "../../Bindings/Awaiter.svelte";
  import { fetchAndInterpret } from "../../Bindings/Client.svelte";
  import Button, { ButtonClass } from "../../Widgets/Button.svelte";
  import { enabled as fileCreationDialog } from "../FileCreationDialog.svelte";
  import { enabled as folderCreationDialog } from "../FolderCreationDialog.svelte";

  export let currentFileId: number | null;
  export let selectedFiles: number[];
  export let onRefresh: () => void;
</script>

<div class="controls">
  <Awaiter
    callback={() =>
      fetchAndInterpret(
        `/file/${currentFileId != null ? `:${currentFileId}` : "!root"}`,
      )}
  >
    <svelte:fragment slot="loading">
      <div class="loading"></div>
    </svelte:fragment>
    <svelte:fragment slot="success" let:result={file}>
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
      {#if currentFileId == null || file?.type === 1}
        <Button
          buttonClass={ButtonClass.PrimaryContainer}
          outline={false}
          onClick={onRefresh}
        >
          <RefreshCwIcon />
          <p>Refresh</p>
        </Button>
      {/if}
      {#if selectedFiles.length !== 0}
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
      {#if selectedFiles.length === 1}
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
      {#if selectedFiles.length >= 1}
        <Button
          buttonClass={ButtonClass.PrimaryContainer}
          outline={false}
          onClick={() => {}}
        >
          <TrashIcon />
          <p>Move to Trash</p>
        </Button>
      {/if}
    </svelte:fragment>
  </Awaiter>
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
