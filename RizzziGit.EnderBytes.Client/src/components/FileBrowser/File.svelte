<script lang="ts">
  import { goto } from "$app/navigation";
  import type { Client } from "$lib/client/client";
  import { RootState } from "$lib/states/root-state";
  import { onMount } from "svelte";
  import Loading from "../Loading.svelte";

  export let client: Client;
  export let fileId: number;
  export let selected: boolean = false;

  export let onClick: () => void;

  const rootState = RootState.state;
  let hovered: boolean = false;

  let loadPromise: Promise<any> | null = null;
  async function load(): Promise<any> {
    await new Promise<void>((resolve) => setTimeout(resolve, 1000))
    return await client.getFile(fileId);
  }

  onMount(() => (loadPromise = load()));
</script>

<button
  class="file-entry {selected ? 'selected' : ''}"
  on:pointerenter={() => (hovered = true)}
  on:pointerleave={() => (hovered = false)}
  on:click={onClick}
  on:dblclick={() => goto("/app/files/" + fileId)}
>
  <div class="overlay">
    {#if selected || hovered}
      <input type="checkbox" disabled checked={selected} />
    {/if}
  </div>
  <div class="base">
    {#if loadPromise == null}
      <div class="file-preview" style="padding: 16px">
        <Loading></Loading>
      </div>
    {:else}
      {#await loadPromise}
        <div class="file-preview" style="padding: 16px">
          <Loading></Loading>
        </div>
      {:then}
        <div class="file-preview">
          <img class="file-preview" src="/favicon.svg" alt="asd" />
        </div>
      {/await}
    {/if}
    <div class="file-info">
      <span class="file-name">
        {#if loadPromise == null}
          Loading...
        {:else}
          {#await loadPromise}
            Loading...
          {:then file}
            {file.Name}
          {/await}
        {/if}</span
      >
    </div>
  </div>
</button>

<style lang="scss">
  button.file-entry:hover {
    > div.base {
      > div.file-info {
        > span.file-name {
          text-decoration: underline;
        }
      }
    }
  }

  button.file-entry {
    background-color: var(--backgroundVariant);
    border: solid 1px transparent;
    cursor: pointer;

    padding: 8px;

    display: flex;
    flex-direction: column;

    > div.overlay {
      width: 100%;
      height: 0px;
      z-index: 0;

      display: flex;
      flex-direction: column;
      align-items: last baseline;
    }

    > div.base {
      > div.file-preview {
        width: 128px;
        height: 128px;

        padding: 8px;
        box-sizing: border-box;

        > img {
          width: 100%;
          height: 100%;
        }
      }

      > div.file-info {
        width: 128px;
        display: flex;
        flex-direction: row;

        > span.file-name {
          font-weight: bold;

          text-align: center;
          text-overflow: ellipsis;

          overflow: hidden;
          white-space: nowrap;

          flex-grow: 1;
        }
      }
    }
  }

  button.file-entry.selected {
    border-color: var(--primaryContainer);
    background-color: var(--primary);
    color: var(--onPrimary);
  }

  button.file-entry:hover {
    border-color: var(--primary);
  }
</style>
