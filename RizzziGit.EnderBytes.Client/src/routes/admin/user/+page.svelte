<script lang="ts" context="module">
</script>

<script lang="ts">
  import type { QueryOptions } from '$lib/server/db';
  import type { User, UserManager } from '$lib/server/db/user';
  import { writable, type Writable } from 'svelte/store';

  import Page from '../+page.svelte';
  import { listUsers } from '$lib/client/api-functions';

  const userQueryOptions: Writable<QueryOptions<UserManager, User>> = writable({
    limit: 10
  });

  const list: Writable<User[]> = writable([]);
  const addToList = (items: User[]) => {
    list.update((value) => {
      value.push(...items);
      return value;
    });

    return $list;
  };

  userQueryOptions.subscribe((value) => {
    list.set([]);
  });

  let noMore: boolean = false;

  async function loadItems(): Promise<void> {
    if (noMore) {
      return;
    }

    const items = await listUsers({
      ...$userQueryOptions,

      offset: $list.length
    });

    if (items.length === 0) {
      noMore = true;
    }

    addToList(items);
  }
</script>

<Page>
  <div class="panel side-panel"></div>
  <div class="panel main-panel"></div>
</Page>

<style lang="scss">
  div.panel {
    background-color: var(--backgroundVariant);
    color: var(--onBackgroundVariant);

    border-radius: 8px;
  }

  div.main-panel {
    flex-grow: 1;
  }

  div.side-panel {
    min-width: 256px;
    max-width: 256px;
  }
</style>
