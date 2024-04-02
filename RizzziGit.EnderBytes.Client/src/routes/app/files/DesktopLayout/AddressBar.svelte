<script lang="ts">
  import { goto } from "$app/navigation";
  import { RootState } from "$lib/states/root-state";
  import { FolderIcon } from "svelte-feather-icons";

  const rootState = RootState.state;
  const appState = $rootState.appState;
  const fileState = $appState.fileState;

  $: currentPath = $fileState.currentFileId ? [$fileState.currentFileId] : [];
  $: console.log($fileState.currentFileId);

  const strings: string[] = ["Root", "School", "Test"];

  fileState.subscribe(console.log);
</script>

<div class="address">
  <FolderIcon />
  {#each strings as string}
    <div class="path-entry">
      <button><p>></p></button>
      <button><p>{string}</p></button>
    </div>
  {/each}
</div>

<style lang="scss">
  div.address {
    background-color: var(--primaryContainer);

    display: flex;
    align-items: center;
    padding: 0px 8px 0px 8px;

    > div.path-entry {
      display: flex;
    }

    button {
      background-color: var(--primaryContainer);
      color: var(--onPrimaryContainer);
      margin: 4px 0px 4px 0px;

      cursor: pointer;

      border-style: solid;
      border-width: 1px;
      border-color: transparent;
      border-radius: 8px;

      > p {
        margin: 4px 2px 4px 2px;
      }
    }

    button:hover {
      border-color: var(--primary);
    }

    button:active {
      border-color: transparent;
      background: var(--primary);
      color: var(--onPrimary);
    }
  }
</style>
