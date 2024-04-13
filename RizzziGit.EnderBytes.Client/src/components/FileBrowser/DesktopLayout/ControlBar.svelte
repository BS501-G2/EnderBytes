<script lang="ts">
  import type { Client } from "$lib/client/client";

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
  import FolderCreationDialog from "../FolderCreationDialog.svelte";
  import Awaiter from "../../Bindings/Awaiter.svelte";

  export let client: Client;
  export let currentFileId: number | null;
  export let selectedFileIds: number[];
  export let onRefresh: () => void;

  let folderCreation: boolean = false;
</script>

<div class="controls">
  {#key currentFileId}
    <Awaiter callback={() => client.getFile(currentFileId)}>
      <svelte:fragment slot="loading">
        <div class="loading"></div>
      </svelte:fragment>
      <svelte:fragment slot="success" let:result={file}>
        <div class="button" title="Upload">
          <button on:click={() => {}}>
            <UploadIcon />
            <p>Upload</p>
          </button>
        </div>
        <div class="button" title="New Folder">
          <button
            on:click={() => {
              folderCreation = true;
            }}
          >
            <FolderPlusIcon />
            <p>New Folder</p>
          </button>
        </div>
        <div class="divider"></div>
        {#if currentFileId == null || file?.Type === 1}
          <div class="button" title="Refresh">
            <button on:click={onRefresh}>
              <RefreshCwIcon />
              <p>Refresh</p>
            </button>
          </div>
        {/if}
        {#if selectedFileIds.length !== 0}
          <div class="button" title="Move To">
            <button>
              <ScissorsIcon />
              <p>Move To</p>
            </button>
          </div>
          <div class="button" title="Copy To">
            <button>
              <CopyIcon />
              <p>Copy To</p>
            </button>
          </div>
        {/if}
        {#if selectedFileIds.length === 1}
          <div class="button" title="Share">
            <button>
              <ShareIcon />
              <p>Share</p>
            </button>
          </div>
          <div class="button" title="Manage Accesss">
            <button>
              <UsersIcon />
              <p>Manage Access</p>
            </button>
          </div>
        {/if}
        {#if selectedFileIds.length >= 1}
          <div class="button" title="Move to Trash">
            <button>
              <TrashIcon />
              <p>Move to Trash</p>
            </button>
          </div>
        {/if}
      </svelte:fragment>
    </Awaiter>
  {/key}
</div>

{#if folderCreation}
  <FolderCreationDialog bind:currentFileId bind:client onCancel={() => (folderCreation = false)} />
{/if}

<style lang="scss">
  div.controls {
    background-color: var(--background);

    display: flex;
    flex-direction: row;
    gap: 8px;
    align-items: center;

    max-height: 64px;
    min-height: 64px;

    padding: 16px;
    box-sizing: border-box;

    border-bottom: solid 1px var(--primaryContainer);

    > div.divider {
      min-width: 1px;
      max-width: 1px;
      height: 100%;
      background-color: var(--onBackground);
      margin: 4px 0px 4px 0px;
    }

    > div.button {
      > button {
        cursor: pointer;

        background-color: unset;
        color: var(--onBackground);

        display: flex;
        align-items: center;
        justify-content: center;
        flex-wrap: wrap;

        gap: 8px;

        border-width: 1px;
        border-color: transparent;
        border-radius: 8px;

        padding: 8px;

        transition: all linear 150ms;

        > p {
          margin: 0px;
        }
      }

      > button:hover {
        border-color: var(--primary);
      }

      > button:active {
        background-color: var(--primary);
        color: var(--onPrimary);
      }

      > button:disabled {
        color: gray;
        border-color: transparent;
        background-color: var(--background);
        cursor: default;
      }
    }
  }
</style>
