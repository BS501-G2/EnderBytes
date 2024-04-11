<script lang="ts">
  import type { Client } from "$lib/client/client";
  import { RootState } from "$lib/states/root-state";
  import { onMount } from "svelte";

  import {
    PlusCircleIcon,
    ScissorsIcon,
    CopyIcon,
    TrashIcon,
    ShareIcon,
    UsersIcon,
    RefreshCwIcon,
  } from "svelte-feather-icons";

  export let client: Client;
  export let currentFileId: number | null;
  export let selectedFileIds: number[];
  export let fileCreationDialog: boolean;
  export let onRefresh: () => void;

  let file: any | null;

  async function update() {
    if (currentFileId != null) {
      file = await client.getFile(currentFileId);

      console.log(file)
    }
  }

  onMount(() => {
    update();
  });
</script>

<div class="controls">
  <div class="button" title="Create">
    <button on:click={() => (fileCreationDialog = true)}>
      <PlusCircleIcon />
      <p>Create</p>
    </button>
  </div>
  <div class="divider"></div>
  {#if currentFileId == null ||  file?.Type === 1}
    <div class="button" title="Refresh">
      <button on:click={onRefresh}>
        <RefreshCwIcon />
        <p>Refresh</p>
      </button>
    </div>
  {/if}
  <div class="button" title="Move To">
    <button disabled={selectedFileIds.length == 0}>
      <ScissorsIcon />
      <p>Move To</p>
    </button>
  </div>
  <div class="button" title="Copy To">
    <button disabled={selectedFileIds.length == 0}>
      <CopyIcon />
      <p>Copy To</p>
    </button>
  </div>
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
  <div class="button" title="Move to Trash">
    <button>
      <TrashIcon />
      <p>Move to Trash</p>
    </button>
  </div>
</div>

<style lang="scss">
  div.controls {
    padding: 16px;

    background-color: var(--background);

    display: flex;
    flex-direction: row;
    gap: 8px;

    border-bottom: solid 1px var(--primaryContainer);

    > div.divider {
      width: 1px;
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
