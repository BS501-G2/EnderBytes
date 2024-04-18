<script lang="ts">
  import { FolderIcon } from "svelte-feather-icons";

  import AddressBarEntry from "./AddressBar/AddressBarEntry.svelte";
  import Awaiter from "../../Bindings/Awaiter.svelte";
  import LoadingSpinner from "../../Widgets/LoadingSpinner.svelte";
  import { fetchAndInterpret } from "../../Bindings/Client.svelte";

  export let currentFileId: number | null;

  async function update(): Promise<any[]> {
    const rootFolder = await fetchAndInterpret("/file/!root");

    const newFiledIds: any[] = [];

    let file = await fetchAndInterpret(
      `/file/${currentFileId != null ? `:${currentFileId}` : "!root"}`,
    );

    while (file.id != rootFolder.id) {
      newFiledIds.unshift(file);
      file = await fetchAndInterpret(
        `/file/${file.parentId != null ? `:${file.parentId}` : "!root"}`,
      );
    }

    return newFiledIds;
  }
</script>

<div class="address">
  <div class="address-content">
    <div class="address-icon">
      <FolderIcon size="100%" />
    </div>
    <Awaiter callback={async () => update()}>
      <svelte:fragment slot="loading">
        <LoadingSpinner size={18} />
      </svelte:fragment>
      <svelte:fragment slot="success" let:result={files}>
        {#each [null, ...files] as file, index}
          <AddressBarEntry {file} {index} length={files.length} />
        {/each}
      </svelte:fragment>
    </Awaiter>
  </div>
</div>

<style lang="scss">
  div.address {
    // background-color: var(--primaryContainer);
    // color: var(--primary);

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

      background-color: var(--backgroundVariant);
      border-radius: 8px;

      flex-grow: 1;

      display: flex;
      align-items: center;
      padding: 0px 8px 0px 8px;
      height: calc(32px);

      // max-width: max(75%, 640px);

      margin: auto;

      overflow-x: auto;
      overflow-y: hidden;
    }
  }
</style>
