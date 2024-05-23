<script lang="ts">
  import { fly, scale } from 'svelte/transition';
  import type { FileBrowserState, FileResource } from '../../../file-browser.svelte';
  import { AnimationFrame, Button, ButtonClass } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';
  import { type Snippet } from 'svelte';

  let {
    fileBrowserState,
    selection
  }: {
    fileBrowserState: Writable<
      FileBrowserState & {
        isLoading: false;
        file: FileResource & { isFolder: false };
      }
    >;
    selection: Writable<FileResource[]>;
  } = $props();

  const viewState: Writable<[lastHover: number, currentTime: number]> = writable([
    Date.now(),
    Date.now()
  ]);

  const mainView: Writable<HTMLElement | null> = writable(null);

  $selection = [$fileBrowserState.file];
</script>

<AnimationFrame
  callback={() => {
    $mainView = $mainView;
    $viewState = [$viewState[0], Date.now()];
  }}
/>

{#snippet expandable(snippet: Snippet)}
  {#if $viewState[0] > $viewState[1] - 5000}
    {@render snippet()}
  {/if}
{/snippet}

{#snippet topBar()}
  <div class="top-bar-container" transition:fly|global={{ duration: 200, y: -32 }}>
    <div class="top-bar">
      <Button outline={false} onClick={() => history.back()}>
        <i class="icon fa-solid fa-arrow-left"></i>
      </Button>
      <h3 class="file-name">{$fileBrowserState.file.name}</h3>
      <Button
        onClick={() => {
          if (document.fullscreenElement != $mainView) {
            $mainView?.requestFullscreen();
          } else {
            document.exitFullscreen();
          }
        }}
      >
        {#if $mainView != document.fullscreenElement}
          <i class="icon fa-solid fa-expand"></i>
        {:else}
          <i class="icon fa-solid fa-compress"></i>
        {/if}
      </Button>
    </div>

    <div class="divider"></div>
  </div>
{/snippet}

<div
  bind:this={$mainView}
  class="file-view{document.fullscreenElement == $mainView ? ' fullscreen' : ''}"
  role="document"
  onmousemove={() => {
    $viewState[0] = Date.now();

    $viewState = $viewState;
  }}
  transition:scale|global={{ duration: 200, start: 0.95 }}
>
  {@render expandable(topBar)}
</div>

<style lang="scss">
  i.icon {
    margin: 8px;
  }

  div.file-view {
    -webkit-app-region: no-drag;

    flex-grow: 1;
    min-height: 0px;

    border-radius: 8px;

    background-color: var(--primary);
    color: var(--onPrimary);

    display: flex;
    flex-direction: column;

    overflow: hidden;
  }

  div.file-view.fullscreen {
    border-radius: 0px;
  }

  div.top-bar-container {
    max-height: 0px;

    > div.top-bar {
      gap: 8px;
      padding: 8px;

      display: flex;
      flex-direction: row;
      align-items: center;

      > h3.file-name {
        flex-grow: 1;
        min-width: 0px;

        overflow: hidden;
        text-overflow: ellipsis;
        white-space: nowrap;
      }
    }
  }

  div.divider {
    min-height: 1px;
    max-height: 1px;

    background-color: var(--onPrimary);
  }
</style>
