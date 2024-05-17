<script lang="ts" context="module">
  export interface PathChainMenu {
    fileId: number;
    currentTarget: HTMLElement;
    forward: boolean;
  }
</script>

<script lang="ts">
  import { goto } from '$app/navigation';

  import {
    Awaiter,
    LoadingSpinner,
    Overlay,
    OverlayPositionType
  } from '@rizzzi/svelte-commons';

  import { scale } from 'svelte/transition';
  import { getFile, scanFolder, type FileResource } from '../../../../file-browser.svelte';
  import PathChainEntry from './entry.svelte';

  const {
    pathChainMenu,
    pathChainMenus = $bindable()
  }: { pathChainMenu: PathChainMenu; pathChainMenus: PathChainMenu[] } = $props();

  function dismiss() {
    const index = pathChainMenus.indexOf(pathChainMenu);
    if (index >= 0) {
      pathChainMenus.splice(index, 1);
    }
  }
</script>

<Overlay
  position={[
    OverlayPositionType.Offset,
    pathChainMenu.currentTarget.getBoundingClientRect().right,
    Math.min(
      pathChainMenu.currentTarget.getBoundingClientRect().top - 1,
      Math.max(window.innerHeight - 128, 0)
    )
  ]}
  onDismiss={dismiss}
>
  <div class="path-chain-menu" transition:scale={{ duration: 200, start: 0.95 }}>
    <Awaiter
      callback={async (): Promise<FileResource[]> => {
        const parentFolder = await getFile(pathChainMenu.fileId);

        return await scanFolder(parentFolder);
      }}
    >
      {#snippet loading()}
          <p class="note"><LoadingSpinner size="1em" /> Loading...</p>
      {/snippet}
      {#snippet error()}
        {(dismiss(), '')}
      {/snippet}
      {#snippet success({ result })}
        {#if result.length == 0}
          <p class="note"><i class="fa-solid fa-border-none"></i> No files</p>
        {/if}
        {#each result as file}
          <PathChainEntry
            {file}
            forward={pathChainMenu.forward}
            onMenu={({ currentTarget }) => {
                if (currentTarget == null || file.parentId == null) {
                  return
                }

                pathChainMenus.push({ forward: true, fileId: file.id, currentTarget: currentTarget as HTMLElement })
              }}
            onClick={() => goto(`/app/files?id=${file.id}`)}
          />
        {/each}
      {/snippet}
    </Awaiter>
  </div>
</Overlay>

<style lang="scss">
  div.path-chain-menu {
    display: flex;
    flex-direction: column;

    user-select: none;

    max-height: 100%;
    max-width: 256px;
    overflow: hidden auto;

    background-color: var(--backgroundVariant);
    box-shadow: 0px 0px 4px var(--shadow);

    border-radius: 8px;

    > p.note {
      color: var(--onBackgroundVariant);
      display: flex;
      align-items: center;
      padding: 4px 8px;
      gap: 8px;

      min-width: 0;
      max-width: 100%;

      text-align: left;
      text-overflow: ellipsis;
      text-wrap: nowrap;
    }
  }
</style>
