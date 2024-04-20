<script lang="ts">
  import { RootState } from "$lib/states/root-state";

  import File from "../../File.svelte";
  import type {
    FileBrowserInformation,
    FileBrowserSelection,
  } from "../../../FileBrowser.svelte";
  import LoadingSpinnerPage from "../../../Widgets/LoadingSpinnerPage.svelte";
    import { hasKeys } from "../../../Bindings/Keyboard.svelte";

  const rootState = RootState.state;

  export let selection: FileBrowserSelection;
  export let info: FileBrowserInformation | null;

  $: files = info?.files ?? [];
</script>

<div
  class="file-list"
  role="none"
  on:click={(event) => {
    if (event.target == event.currentTarget) {
      $selection = [];
    }
  }}
>
  {#if info == null}
    <LoadingSpinnerPage />
  {:else}
    {#each files as file, index}
      <File
        {file}
        selected={$selection.includes(file)}
        onClick={() => {
          if (hasKeys("control")) {
            $selection = !$selection.includes(file)
              ? [...$selection, file]
              : $selection.filter((id) => id !== file);
          } else if (hasKeys("shift")) {
            if ($selection.length === 0) {
              $selection = [file];
            } else {
              const startIndex = files.indexOf($selection[0]);
              const endIndex = index;

              if (startIndex > endIndex) {
                $selection = files.slice(endIndex, startIndex + 1).toReversed();
              } else {
                $selection = files.slice(startIndex, endIndex + 1);
              }
            }
          } else if ($selection.length !== 1 || $selection[0] !== file) {
            $selection = [file];
          } else {
            $selection = [];
          }
        }}
      />
    {/each}
  {/if}
</div>

<style lang="scss">
  div.file-list {
    background-color: var(--backgroundVariant);
    border-radius: 16px;

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
</style>
