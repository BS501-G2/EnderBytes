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
  import { Button, ButtonClass, Overlay, OverlayPositionType } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';
  import { fly } from 'svelte/transition';

  const sortValues: [
    type: NonNullable<FolderListFilter['sort']> | undefined,
    name: string,
    icon: string,
    description: string
  ][] = [
    [undefined, 'None', 'fa-solid fa-sort', 'Do not sort files.'],
    ['name', 'Name', 'fa-solid fa-sort-alpha-up', 'Sort files by name.'],
    ['ctime', 'Created', 'fa-solid fa-sort-numeric-up', 'Sort files by creation time.'],
    ['utime', 'Modified', 'fa-solid fa-sort-numeric-down', 'Sort files by modification time.']
  ];

  const descValues: [type: boolean | undefined, name: string, icon: string, description: string][] =
    [
      [undefined, 'None', 'fa-solid fa-sort', 'Do not sort files.'],
      [true, 'Descending', 'fa-solid fa-sort-down', 'Sort files in descending order.'],
      [false, 'Ascending', 'fa-solid fa-sort-up', 'Sort files in ascending order.']
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
      <div>
        <b>Sort</b>
        <div class="row">
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
      </div>
      <div>
        <b>Order By</b>
        <div class="row">
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
    </div>
  </Overlay>
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

    > div {
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
    }
  }
</style>
