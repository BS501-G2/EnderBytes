<script lang="ts">
  import { RootState } from "$lib/states/root-state";

  import File from "../../File.svelte";
  import FileDetails from "./FileDetails.svelte";
  import Awaiter from "../../../Bindings/Awaiter.svelte";
  import ClientAwaiter from "../../../Bindings/ClientAwaiter.svelte";

  const rootState = RootState.state;
  const keyboardState = $rootState.keyboardState;

  export let file: any;
  export let selectedFileIds: number[];
  export let onRefresh: (autoLoad?: boolean | undefined) => Promise<void>;
</script>

<ClientAwaiter let:client>
  <Awaiter callback={() => client.scanFolder(file.Id)} bind:reset={onRefresh}>
    <svelte:fragment slot="success" let:result={fileIds}>
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
                  const startIndex = fileIds.indexOf(selectedFileIds[0]);
                  const endIndex = index;

                  if (startIndex > endIndex) {
                    selectedFileIds = fileIds
                      .slice(endIndex, startIndex + 1)
                      .toReversed();
                  } else {
                    selectedFileIds = fileIds.slice(startIndex, endIndex + 1);
                  }
                }
              } else if (
                selectedFileIds.length !== 1 ||
                selectedFileIds[0] !== fileId
              ) {
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
      <FileDetails bind:selectedFileIds />
    </svelte:fragment>
  </Awaiter>
</ClientAwaiter>

<style lang="scss">
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
</style>
