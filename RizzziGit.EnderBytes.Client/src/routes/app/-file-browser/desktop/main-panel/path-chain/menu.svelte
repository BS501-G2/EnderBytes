<script lang="ts" context="module">
  export interface PathChainMenu {
    fileId: number;
    currentTarget: HTMLElement;
    forward: boolean;
  }
</script>

<script lang="ts">
  import { goto } from '$app/navigation';

  import { Awaiter, LoadingSpinner, Overlay, OverlayPositionType } from '@rizzzi/svelte-commons';

  import { fly } from 'svelte/transition';
  import PathChainEntry from './entry.svelte';
  import { getFile, scanFolder } from '$lib/client/api-functions';
  import type { File } from '$lib/server/db/file';

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
  <div class="path-chain-menu" transition:fly|global={{ duration: 200, x: -32 }}>
    <Awaiter
      callback={async (): Promise<File[]> => {
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
                if (currentTarget == null || file.parentFileId == null) {
                  return
                }

                pathChainMenus.push({ forward: true, fileId: file.id, currentTarget: currentTarget as HTMLElement })
              }}
            onClick={() => {
              goto(`/app/files?id=${file.id}`);
              pathChainMenus.splice(0, pathChainMenus.length);
            }}
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
