<script lang="ts">
  import { FolderIcon } from "svelte-feather-icons";

  import { RootState } from "$lib/states/root-state";

  import Overlay from "../../Overlay.svelte";
  import type { Client } from "$lib/client/client";
  import { goto } from "$app/navigation";
  import Loading from "../../Loading.svelte";

  export let client: Client;
  export let currentFileId: number | null;

  let fileIds: number[] = [];

  let menuOffsetX: number = 0;
  let menuOffsetY: number = 0;
  let menuFileIdsPromise: Promise<number[]> | null = null;

  async function update() {
    const rootFolderId = await client.getRootFolderId();
    const storageId = await client.getOwnStorageId();

    let file = await client.getFile(currentFileId || rootFolderId);

    const newFiledIds = [];

    if (file.StorageId == storageId) {
      while (file.Id != rootFolderId) {
        newFiledIds.unshift(file.Id);
        file = await client.getFile(file.ParentId);
      }

      fileIds = newFiledIds;
    }
  }

  $: {
    currentFileId;
    update();
  }
</script>

<div class="address">
  <div class="address-content">
    <div class="address-icon">
      <FolderIcon size="100%" />
    </div>
    {#each [null, ...fileIds] as fileId, index}
      <div class="path-entry">
        {#if fileId == null}
          <button on:click={() => goto("/app/files")}><p>/</p></button>
        {:else}
          <button on:click={() => goto(`/app/files/${fileId}`)}
            ><p>
              {#await client.getFile(fileId)}
                ...
              {:then file}
                {file.Name}
              {:catch}
                [error]
              {/await}
            </p></button
          >
        {/if}
        {#if index != fileIds.length}
          <button
            on:click={(e) => {
              menuOffsetX =
                e.currentTarget.offsetLeft +
                e.currentTarget.clientWidth -
                (e.currentTarget.parentElement?.parentElement?.scrollLeft ?? 0);
              menuOffsetY = e.currentTarget.offsetTop - 4;
              menuFileIdsPromise = client.scanFolder(fileId);
            }}><p>></p></button
          >
        {/if}
      </div>
    {/each}
  </div>

  {#if menuFileIdsPromise != null}
    <Overlay
      offsetX={menuOffsetX}
      offsetY={menuOffsetY}
      onDismiss={() => (menuFileIdsPromise = null)}
    >
      <div class="menu">
        {#await menuFileIdsPromise}
          <div class="loading-icon">
            <Loading />
          </div>
        {:then menuFileIds}
          {#each menuFileIds as fileId}
            <button
              on:click={() => {
                menuFileIdsPromise = null;
                goto(`/app/files/${fileId}`);
              }}
            >
              <FolderIcon />
              <p>
                {#await client.getFile(fileId)}
                  [loading...]
                {:then file}
                  {file.Name}
                {:catch}
                  [error]
                {/await}
              </p></button
            >
          {/each}
        {/await}

        <!-- <button
          ><FolderIcon />
          <p>asd</p></button
        >
        <button
          ><FolderIcon />
          <p>asd asdf</p></button
        >
        <button
          ><FolderIcon />
          <p>asda sdf</p></button
        >
        <button
          ><FolderIcon />
          <p>asda sdf asdf</p></button
        > -->
      </div></Overlay
    >{/if}
</div>

<style lang="scss">
  div.address {
    background-color: var(--primaryContainer);
    color: var(--primary);
    padding: 8px;

    min-height: 32px;
    max-height: 32px;

    display: flex;
    align-items: center;

    > div.address-content {
      > div.address-icon {
        min-width: calc(32px - 16px);
        min-height: calc(32px - 16px);
        max-width: calc(32px - 16px);
        max-height: calc(32px - 16px);

        margin-right: 8px;
      }

      background-color: var(--background);

      flex-grow: 1;

      display: flex;
      align-items: center;
      padding: 0px 8px 0px 8px;
      height: calc(32px);

      border-style: solid;
      border-width: 1px;
      border-color: var(--primary);

      max-width: max(75%, 640px);

      margin: auto;

      overflow-x: auto;
      overflow-y: hidden;

      > div.path-entry {
        display: flex;
      }
    }

    button {
      background-color: transparent;
      color: var(--primary);
      margin: 4px 0px 4px 0px;

      cursor: pointer;

      border-style: solid;
      border-width: 1px;
      border-color: transparent;
      // border-radius: 8px;
      user-select: none;

      > p {
        margin: 4px 2px 4px 2px;

        font-style: italic;
      }

      display: flex;
      gap: 8px;
    }

    button:hover {
      border-color: var(--onPrimaryContainer);
    }

    button:active {
      border-color: transparent;
      background: var(--primary);
      color: var(--onPrimary);
      border-color: var(--primary);
    }

    div.menu {
      display: flex;
      flex-direction: column;

      background-color: var(--background);

      padding: 4px;
      // border-radius: 8px;
      border-style: solid;
      border-width: 1px;
      border-color: var(--primary);

      > div.loading-icon {
        max-width: 72px;
        max-height: 72px;
        min-width: 72px;
        min-height: 72px;

        box-sizing: border-box;
        padding: 16px;
      }

      > button {
        text-align: start;
      }
    }
  }
</style>
