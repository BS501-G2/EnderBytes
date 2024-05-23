<script lang="ts">
  import { Awaiter, Title } from '@rizzzi/svelte-commons';
  import { getFile } from '../app/file-browser.svelte';
  import { apiFetch } from '$lib/client.svelte';

  async function callback(): Promise<any> {
    const file = await getFile();
    const fileContent = apiFetch({ path: `/file/:${file.id}/content` });

    return fileContent;
  }
</script>

<div class="container">
  <Title title="Testing Ground">
    {#snippet children(title: string)}
      <h2 class="title">{title}</h2>
    {/snippet}
  </Title>

  <Awaiter {callback}>
    {#snippet success({ result })}
      <pre class="code">{JSON.stringify(result, undefined, '  ')}</pre>
    {/snippet}
  </Awaiter>
</div>

<style lang="scss">
  div.container {
    padding: 16px;
  }
</style>
