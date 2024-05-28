<script lang="ts" context="module">
  export interface PageData {}
</script>

<script lang="ts">
  import { clientSideInvoke } from '$lib/client/api';

  import { Awaiter, AnimationFrame} from '@rizzzi/svelte-commons';
  import { onMount } from 'svelte';
  import { BSON } from 'bson';

  const {}: {} = $props();

  // async function req () {
  //   clientSideInvoke('testFunction', 'asd')

  //   setTimeout(req, 1)
  // }

  // req()
</script>

<Awaiter
  callback={async () => {
    const list: Promise<any>[] = []

    for (let index = 0; index < 100; index++) {
      list.push(clientSideInvoke('testFunction', 'asd'))
    }

    return Promise.all(list);
  }}
>
  {#snippet success({ result })}
    {JSON.stringify(result)}
  {/snippet}
  {#snippet error({ error })}
    {(console.error(error), '')}
    {error.message}
  {/snippet}
</Awaiter>
