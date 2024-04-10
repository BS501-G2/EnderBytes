<script lang="ts">
  import type { Client } from "$lib/client/client";
  import { RootState } from "$lib/states/root-state";
  import { onMount } from "svelte";
  import LoadingPage from "../../../LoadingPage.svelte";

  import File from "../../File.svelte";
  import { FileIcon } from "svelte-feather-icons";

  const rootState = RootState.state;
  const keyboardState = $rootState.keyboardState;

  export let client: Client;
  export let file: any;
  export let selectedFileIds: number[] = [];

  let loadPromise: Promise<number[]> | null;
  async function load(): Promise<number[]> {
    return await client.scanFolder(file.Id);
  }

  onMount(() => (loadPromise = load()));
</script>

{#if loadPromise == null}
  <LoadingPage />
{:else}
  {#await loadPromise}
    <LoadingPage />
  {:then fileIds}
    <div class="file-list">
      {#each fileIds as fileId, index}
        <File
          {client}
          {fileId}
          selected={selectedFileIds.includes(fileId)}
          onClick={() => {
            if ($keyboardState.hasKeys("control")) {
              selectedFileIds = !selectedFileIds.includes(fileId)
                ? [...selectedFileIds, fileId]
                : selectedFileIds.filter((id) => id !== fileId);
            } else if ($keyboardState.hasKeys("shift")) {
              if (selectedFileIds.length === 0) {
                selectedFileIds = [fileId];
              } else {
                const startIndex = selectedFileIds[0];
                const endIndex = index;

                if (startIndex > endIndex) {
                  selectedFileIds = fileIds
                    .slice(endIndex, startIndex + 1)
                    .toReversed();
                } else {
                  selectedFileIds = fileIds.slice(startIndex, endIndex + 1);
                }
              }
            } else if (selectedFileIds.length === 0) {
              selectedFileIds = [fileId];
            } else {
              selectedFileIds = [];
            }

            selectedFileIds = selectedFileIds;
          }}
        />
      {/each}
    </div>

    <div class="divider"></div>

    <div class="file-details">
      <div class="file-preview">
        {#if selectedFileIds.length > 1}
          <FileIcon />
        {:else}
          <img alt="File preview for `file`" src="/favicon.svg" />
        {/if}
      </div>
      <div class="file-info">
        {#if selectedFileIds.length > 1}
          <p>{selectedFileIds.length} files</p>
        {:else}
          <span class="file-name">File name</span>
          <span class="file-size">File size</span>
        {/if}
      </div>
    </div>
  {:catch}
    <button on:click={() => (loadPromise = load())}>Retry</button>
  {/await}
{/if}

<style lang="scss">
  div.file-details {
    display: flex;
    flex-direction: column;

    > div.file-preview {
      display: flex;
      flex-direction: column;

      width: 256px;
      height: 256px;

      align-items: center;

      margin: 16px;

      > img {
        width: 100%;
        height: 100%;
      }
    }
  }

  div.file-list {
    flex-grow: 1;

    padding: 16px;

    display: flex;
    flex-wrap: wrap;
    align-content: flex-start;
    justify-content: start;
    gap: 8px;

    overflow: auto;
    min-height: 0px;
  }

  div.divider {
    min-width: 1px;
    background-color: var(--primaryContainer);
  }

  div.file-details {
    min-width: 320px;
    max-width: 320px;
    flex-grow: 1;

    padding: 16px;

    display: flex;
  }
</style>
