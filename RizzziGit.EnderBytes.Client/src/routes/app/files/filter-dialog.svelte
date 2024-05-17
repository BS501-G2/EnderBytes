<script lang="ts" context="module">
  export interface FolderListFilter {
    sort?: 'name' | 'ctime' | 'utime';
    desc?: boolean;
    offset?: number;
  }
</script>

<script lang="ts">
  import { Dialog, type AwaiterLoadFunction } from '@rizzzi/svelte-commons';

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

  let {
    filter = $bindable(),
    onFilterApply,
    onDismiss
  }: {
    filter: FolderListFilter;
    onFilterApply: AwaiterLoadFunction;
    onDismiss: () => void;
  } = $props();
</script>

<Dialog {onDismiss}>
  {#snippet head()}
    <h2>Filter</h2>
  {/snippet}
  {#snippet body()}
    <div class="filter-box">
      <p>Select any metric to filter files.</p>
      <div class="filters">
        <div class="filter-entry">
          <p>Sort By:</p>
          <select
            onchange={({ currentTarget }) => {
                filter.sort = currentTarget.value as NonNullable<FolderListFilter['sort']> | null || undefined;
              }}
          >
            {#each sortValues as sortValue}
              <option value={sortValue[0]}>
                {sortValue[1]} - {sortValue[2]}
              </option>
            {/each}
          </select>
        </div>
      </div>
    </div>
  {/snippet}
</Dialog>

<style lang="scss">
  div.filter-box {
    display: flex;
    flex-direction: column;
    gap: 8px;

    > div.filters {
      > div.filter-entry {
        display: flex;
        flex-direction: row;
        gap: 8px;

        > p {
          min-width: 128px;
          max-width: 128px;

          text-overflow: ellipsis;
          text-wrap: nowrap;
          overflow: hidden;
        }
      }
    }
  }
</style>
