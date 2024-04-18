<script lang="ts">
  import { RootState } from "$lib/states/root-state";

  import File from "../../File.svelte";
  import FileDetails from "./FileDetails.svelte";
  import Awaiter from "../../../Bindings/Awaiter.svelte";
  import { fetchAndInterpret } from "../../../Bindings/Client.svelte";

  const rootState = RootState.state;
  const keyboardState = $rootState.keyboardState;

  export let file: any;
  export let selectedFiles: any[];
  export let onRefresh: (autoLoad?: boolean | undefined) => Promise<void>;
</script>

<Awaiter
  callback={() => fetchAndInterpret(`/file/:${file.id}/files`)}
  bind:reset={onRefresh}
>
  <svelte:fragment slot="success" let:result={files}>
    <div class="file-list">
      {#each files as file, index}
        <File
          {file}
          selected={selectedFiles.includes(file)}
          onClick={() => {
            if ($keyboardState.hasKeys("control")) {
              selectedFiles = !selectedFiles.includes(file)
                ? [...selectedFiles, file]
                : selectedFiles.filter((id) => id !== file);
            } else if ($keyboardState.hasKeys("shift")) {
              if (selectedFiles.length === 0) {
                selectedFiles = [file];
              } else {
                const startIndex = files.indexOf(selectedFiles[0]);
                const endIndex = index;

                if (startIndex > endIndex) {
                  selectedFiles = files
                    .slice(endIndex, startIndex + 1)
                    .toReversed();
                } else {
                  selectedFiles = files.slice(startIndex, endIndex + 1);
                }
              }
            } else if (
              selectedFiles.length !== 1 ||
              selectedFiles[0] !== file
            ) {
              selectedFiles = [file];
            } else {
              selectedFiles = [];
            }

            selectedFiles = selectedFiles;
          }}
        />
      {/each}
    </div>

    <div class="divider"></div>
    <FileDetails bind:selectedFiles />
  </svelte:fragment>
</Awaiter>

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
