<script lang="ts" context="module">
  export type ControlBarItemGroup = 'new' | 'actions' | 'arrangement';

  export interface ControlBarItem {
    label: string;
    icon: string;
    group: ControlBarItemGroup;
    action: (event: MouseEvent) => Promise<void> | void;
    isVisible: (selection: FileResource[]) => boolean;
  }
</script>

<script lang="ts">
  import { Button, ButtonClass } from '@rizzzi/svelte-commons';
  import { scale } from 'svelte/transition';
  import type { FileBrowserState, FileResource } from '../../../file-browser.svelte';
  import type { Writable } from 'svelte/store';

  let {
    fileBrowserState,
    selection = $bindable()
  }: { fileBrowserState: Writable<FileBrowserState>; selection: Writable<FileResource[]> } =
    $props();
</script>

{#snippet buttons(actions: ControlBarItem[])}
  {#each actions as action}
    {#snippet entry(action: ControlBarItem)}
      <i class="icon {action.icon}"></i>
      <p>{action.label}</p>
    {/snippet}

    <div class="button-entry" transition:scale|global={{ duration: 200, start: 0.95 }}>
      <Button
        buttonClass={ButtonClass.BackgroundVariant}
        hint={action.label}
        outline={false}
        onClick={action.action}
      >
        <div class="button">
          {@render entry(action)}
        </div>
      </Button>
    </div>
  {/each}
{/snippet}

{#if $fileBrowserState.controlBarActions != null}
  {@const actions = $fileBrowserState.controlBarActions}

  <div class="control-bar-container">
    <div class="control-bar">
      {#snippet action(actions: ControlBarItem[], group: ControlBarItemGroup)}
        {@const filteredActions =
          actions?.filter((action) => action.group == group && action.isVisible($selection)) ?? []}

        {#if filteredActions.length != 0}
          <div class="control-group" transition:scale|global={{ duration: 200, start: 0.95 }}>
            {@render buttons(filteredActions)}
          </div>
        {/if}
      {/snippet}

      {@render action(actions, 'new')}
      {@render action(actions, 'actions')}
      <div class="spacer"></div>
      {@render action(actions, 'arrangement')}
    </div>
  </div>
{/if}

<style lang="scss">
  div.control-bar-container {
    min-width: 0px;

    min-height: 2em;
    // max-height: 2em;

    overflow: auto hidden;

    box-sizing: border-box;

    display: flex;
    flex-direction: column;

    > div.control-bar {
      gap: 8px;

      display: flex;
      flex-direction: row;
      align-items: center;
      min-height: 2em;
      max-height: 2em;

      > div.spacer {
        flex-grow: 1;
      }

      > div.control-group {
        display: flex;
        flex-direction: row;
        align-items: center;

        background-color: var(--backgroundVariant);
        color: var(--onBackgroundVariant);
        border-radius: 8px;

        min-height: 2em;
        max-height: 2em;
        padding: 0px 2px;
      }
    }
  }

  div.button-entry {
    display: contents;
  }

  div.button {
    display: flex;
    align-items: center;

    min-height: 2em;

    gap: 8px;
  }
  i.icon {
    min-height: 100%;
    max-height: 100%;

    font-size: 1.25em;
  }

  p {
    text-wrap: nowrap;
  }
</style>
