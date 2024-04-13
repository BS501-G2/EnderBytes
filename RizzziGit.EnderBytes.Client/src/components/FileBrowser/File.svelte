<script lang="ts">
  import { goto } from "$app/navigation";
  import type { Client } from "$lib/client/client";

  import Loading from "../Widgets/LoadingSpinner.svelte";

  import { AlertTriangleIcon } from "svelte-feather-icons";
  import Awaiter from "../Bindings/Awaiter.svelte";

  export let client: Client;
  export let fileId: number;
  export let selected: boolean = false;

  export let onClick: () => void;

  let hovered: boolean = false;

  let loadPromise: Promise<Client> | null;
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
      <input type="checkbox" disabled checked={selected} on:click={onClick} />
    {/if}
  </div>
  <div class="base">
    {#key loadPromise}
      <Awaiter callback={() => loadPromise}>
        <svelte:fragment slot="loading">
          <div class="file-preview" style="padding: 16px">
            <Loading></Loading>
          </div>
        </svelte:fragment>
        <svelte:fragment slot="success" let:result={file}>
          <div class="file-preview">
            <img class="file-preview" src="/favicon.svg" alt="asd" />
          </div>
        </svelte:fragment>
        <svelte:fragment slot="error" let:error>
          <div class="file-preview" style="padding: 16px; color: red">
            <AlertTriangleIcon size="100%" />
          </div>
        </svelte:fragment>
      </Awaiter>
    {/key}
    <div class="file-info">
      <span class="file-name">
        <Awaiter callback={() => (loadPromise = client.getFile(fileId))}>
          <svelte:fragment slot="loading">Loading...</svelte:fragment>
          <svelte:fragment slot="success" let:result={file}>
            {file.Name}
          </svelte:fragment>
        </Awaiter>
      </span>
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

    border-radius: 8px;

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
        max-width: 128px;
        max-height: 128px;

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
