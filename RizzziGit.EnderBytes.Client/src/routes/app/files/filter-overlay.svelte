<script lang="ts" context="module">
  export interface FolderListFilter {
    sort?: 'name' | 'ctime' | 'utime';
    desc?: boolean;
    offset?: number;
  }

  export interface FolderListFilterState {
    enabled: [x: number, y: number] | null;
    state: FolderListFilter;
    button: HTMLButtonElement | null;
  }

  export const filterOverlayState: Writable<FolderListFilterState> = writable({
    enabled: null,
    state: {},
    button: null
  });
</script>

<script lang="ts">
  import {
    Overlay,
    OverlayPositionType
  } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';
  import { fly } from 'svelte/transition';

  const sortValues: [
    type: NonNullable<FolderListFilter['sort']> | null,
    name: string,
    description: string
  ][] = [
    [null, 'None', 'Do not sort files.'],
    ['name', 'Name', 'Sort files by name'],
    ['ctime', 'Create Time', 'Sort files by creation time'],
    ['utime', 'Update Time', 'Sort files by update time']
  ];

  const descValues: [
    type: boolean | null,
    name: string,
    description: string
  ][] = [
    [null, 'None', 'Do not sort files.'],
    [true, 'Descending', 'Sort files in descending order.'],
    [false, 'Ascending', 'Sort files in ascending order.']
  ];

  let {
    onFilterApply,
    onDismiss
  }: {
    onFilterApply: () => void;
    onDismiss: () => void;
  } = $props();
</script>

{#if $filterOverlayState.enabled != null}
  {@const {
    enabled: [x, y]
  } = $filterOverlayState}

  <Overlay {onDismiss} position={[OverlayPositionType.Offset, -(x - 2), y + 8]}>
    <div class="filter-overlay" transition:fly|global={{ duration: 200, y: -16 }}>
      <div class="row">

      </div>
    </div>
  </Overlay>
{/if}

<style lang="scss">
  div.filter-overlay {
    background-color: var(--backgroundVariant);
    color: var(--onBackgroundVariant);

    padding: 8px;
    border-radius: 8px;

    box-shadow: var(--shadow) 0px 0px 8px;

    display: flex;
    flex-direction: column;
  }
</style>
