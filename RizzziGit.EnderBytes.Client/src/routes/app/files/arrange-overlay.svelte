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
    state: {
      sort: 'name',
      desc: false
    },
    button: null
  });
</script>

<script lang="ts">
  import {
    Button,
    ButtonClass,
    Dialog,
    Overlay,
    OverlayPositionType,
    ResponsiveLayout,
    ViewMode,
    viewMode
  } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';
  import { fly } from 'svelte/transition';

  const sortValues: [
    type: NonNullable<FolderListFilter['sort']> | undefined,
    name: string,
    icon: string,
    description: string
  ][] = [
    ['name', 'Name', 'fa-solid fa-sort-alpha-up', 'Sort files by name.'],
    ['ctime', 'Created', 'fa-solid fa-sort-numeric-up', 'Sort files by creation time.'],
    ['utime', 'Modified', 'fa-solid fa-sort-numeric-down', 'Sort files by modification time.']
  ];

  const descValues: [type: boolean | undefined, name: string, icon: string, description: string][] =
    [
      [false, 'Ascending', 'fa-solid fa-sort-up', 'Sort files in ascending order.'],
      [true, 'Descending', 'fa-solid fa-sort-down', 'Sort files in descending order.']
    ];

  let {
    onFilterApply
  }: {
    onFilterApply: () => void;
  } = $props();

  function onDismiss() {
    $filterOverlayState.enabled = null;
    $filterOverlayState = $filterOverlayState;
  }
</script>

{#snippet overlay()}
  <div class="filter-container">
    <b>Sort</b>
    <div class="row {$viewMode & ViewMode.Mobile ? 'mobile' : ''}">
      {#each sortValues as [sort, name, icon, description]}
        <Button
          buttonClass={sort == $filterOverlayState.state.sort
            ? ButtonClass.Primary
            : ButtonClass.BackgroundVariant}
          enabled={sort != $filterOverlayState.state.sort}
          onClick={() => {
            $filterOverlayState.state.sort = sort;
            $filterOverlayState = $filterOverlayState;

            onFilterApply();
          }}
          hint={description}
        >
          <div class="button">
            <i class={icon}></i>
            <p>{name}</p>
          </div>
        </Button>
      {/each}
    </div>
    <b>Order By</b>
    <div class="row {$viewMode & ViewMode.Mobile ? 'mobile' : ''}">
      {#each descValues as [desc, name, icon, description]}
        <Button
          buttonClass={desc === $filterOverlayState.state.desc
            ? ButtonClass.Primary
            : ButtonClass.BackgroundVariant}
          enabled={desc !== $filterOverlayState.state.desc}
          onClick={() => {
            $filterOverlayState.state.desc = desc;
            $filterOverlayState = $filterOverlayState;

            onFilterApply();
          }}
          hint={description}
        >
          <div class="button">
            <i class={icon}></i>
            <p>{name}</p>
          </div>
        </Button>
      {/each}
    </div>
  </div>
{/snippet}

{#if $filterOverlayState.enabled != null}
  {@const {
    enabled: [x, y]
  } = $filterOverlayState}

  <ResponsiveLayout>
    {#snippet desktop()}
      <Overlay {onDismiss} position={[OverlayPositionType.Offset, -(x - 2), y + 8]}>
        <div class="filter-overlay" transition:fly|global={{ duration: 200, y: -16 }}>
          {@render overlay()}
        </div>
      </Overlay>
    {/snippet}

    {#snippet mobile()}
      <Dialog {onDismiss}>
        {#snippet head()}
          <h2>Arrange</h2>
        {/snippet}
        {#snippet body()}
          {@render overlay()}
        {/snippet}
      </Dialog>
    {/snippet}
  </ResponsiveLayout>
{/if}

<style lang="scss">
  div.filter-overlay {
    background-color: var(--backgroundVariant);
    color: var(--onBackgroundVariant);

    padding: 8px;
    border-radius: 8px;
    gap: 8px;

    box-shadow: var(--shadow) 0px 0px 8px;

    display: flex;
    flex-direction: column;
  }

  div.filter-container {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 8px;

    > div.row {
      display: flex;
      flex-direction: row;
      align-items: center;
      justify-content: safe center;

      gap: 8px;

      div.button {
        display: flex;
        flex-direction: row;
        align-items: center;
        gap: 8px;

        margin: 4px;
      }
    }

    > div.row.mobile {
      flex-direction: column;
      align-items: stretch;
    }
  }
</style>
