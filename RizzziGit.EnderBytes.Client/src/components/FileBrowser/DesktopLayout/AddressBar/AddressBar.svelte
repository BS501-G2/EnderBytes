<script lang="ts">
  import { FolderIcon } from "svelte-feather-icons";

  import AddressBarEntry from "./AddressBarEntry.svelte";
  import Awaiter from "../../../Bindings/Awaiter.svelte";
  import LoadingSpinner from "../../../Widgets/LoadingSpinner.svelte";
  import ClientAwaiter from "../../../Bindings/ClientAwaiter.svelte";
  import type { Client } from "$lib/client/client";

  export let currentFileId: number | null;

  async function update(client: Client): Promise<number[]> {
    const rootFolderId = await client.getRootFolderId();
    const storageId = await client.getOwnStorageId();

    let file = await client.getFile(currentFileId || rootFolderId);

    const newFiledIds: number[] = [];

    if (file.StorageId == storageId) {
      while (file.Id != rootFolderId) {
        newFiledIds.unshift(file.Id);
        file = await client.getFile(file.ParentId);
      }
    }

    return newFiledIds;
  }
</script>

<div class="address">
  <div class="address-content">
    <div class="address-icon">
      <FolderIcon size="100%" />
    </div>
    <ClientAwaiter let:client>
      {#key currentFileId}
        <Awaiter callback={async () => update(client)}>
          <svelte:fragment slot="loading">
            <LoadingSpinner size={18} />
          </svelte:fragment>
          <svelte:fragment slot="success" let:result={fileIds}>
            {#each [null, ...fileIds] as fileId, index}
              <AddressBarEntry {fileId} {index} length={fileIds.length} />
            {/each}
          </svelte:fragment>
        </Awaiter>
      {/key}
    </ClientAwaiter>
  </div>
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
    }
  }
</style>
