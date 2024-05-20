<script lang="ts">
  import { goto } from '$app/navigation';
    import type { Writable } from 'svelte/store';

  import { type FileBrowserState, type FilePathChainInfo } from '../../../file-browser.svelte';
  import PathChainEntry from './path-chain/entry.svelte';
  import PathChainMenuElement, { type PathChainMenu } from './path-chain/menu.svelte';

  import { LoadingSpinner } from '@rizzzi/svelte-commons';

  const { fileBrowserState }: { fileBrowserState: Writable<FileBrowserState> } = $props();

  let pathChainMenus: PathChainMenu[] = $state([]);
</script>

<div class="path-chain-container">
  <div class="path-chain">
    {#if $fileBrowserState!.isLoading}
      <LoadingSpinner size="1em" />
    {:else}
      <i class="fa-regular fa-{$fileBrowserState!.file?.isFolder ? 'folder' : 'file'}"></i>
      {#each $fileBrowserState!.pathChain!.chain as file}
        <PathChainEntry
          {file}
          onClick={() => goto(`/app/files?id=${file.id}`)}
          onMenu={({ currentTarget }) => {
            if (currentTarget == null || file.parentId == null) {
              return
            }

            pathChainMenus.push({
              fileId: file.parentId,
              forward: true,
              currentTarget: currentTarget as HTMLElement
            });
          }}
        />
      {/each}
    {/if}
  </div>
</div>

{#each pathChainMenus as pathChainMenu}
  <PathChainMenuElement {pathChainMenu} bind:pathChainMenus />
{/each}

<style lang="scss">
  div.path-chain-container {
    background-color: var(--backgroundVariant);
    border-radius: 8px;

    overflow: auto hidden;

    min-width: 0px;
    min-height: 2em;
    max-height: 2em;

    box-sizing: border-box;

    display: flex;

    > div.path-chain {
      min-height: 1em;

      gap: 8px;
      padding: 3.5px 8px;

      display: flex;
      flex-direction: row;
      align-items: center;
    }
  }
</style>
