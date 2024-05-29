<script lang="ts" context="module">
  export interface PageData {}
</script>

<script lang="ts">
  import { type Writable, writable } from 'svelte/store';
  import { clientSideInvoke } from '$lib/client/api';

  import { Awaiter, Button, type AwaiterResetFunction } from '@rizzzi/svelte-commons';
  import type { TestData } from '$lib/server/db/test';

  const b: Writable<number> = writable(0);

  async function run(): Promise<void> {
    const result = await clientSideInvoke('createTest', 'asd');
    $b++;
    const get = <TestData>await clientSideInvoke('getTest', result.id);
    $b++;
    const update = await clientSideInvoke('updateTest', get.id, 'updated');
    $b++;
    const versions = await clientSideInvoke('listTestVersion', get.id);
    $b++;
    await clientSideInvoke('deleteTest', result.id)
    $b++;
  }

  let a: AwaiterResetFunction<TestData[]>;
</script>

<Button
  onClick={async () => {
    await a(true);
  }}
>
  <p>Rerun</p>
</Button>
<Awaiter
  bind:reset={a}
  autoLoad={false}
  callback={async () => {
    const list: Promise<void>[] = []

    for (let index = 0; index < 10000; index++) {
      list.push(run())
    }

    return Promise.all(list);
  }}
>
  {#snippet loading()}
    <p>{$b}</p>
  {/snippet}
  {#snippet success({ result })}{/snippet}
  {#snippet error({ error })}
    {(console.error(error), '')}
    {error.message}
  {/snippet}
</Awaiter>

<style lang="scss">
  p {
    padding: 8px;
  }
</style>
