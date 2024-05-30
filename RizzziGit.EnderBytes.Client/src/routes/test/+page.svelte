<script lang="ts" context="module">
  async function run(): Promise<void> {
    const authentication = (await clientSideInvoke(
      'autenticate',
      'testuser',
      UserKeyType.Password,
      new TextEncoder().encode('TestTest123;')
    ))!;

    const result = await clientSideInvoke('verify', authentication);
    console.log(result)
  }
</script>

<script lang="ts">
  import { clientSideInvoke } from '$lib/client/api';

  import { Button, LoadingSpinner } from '@rizzzi/svelte-commons';
  import { UserKeyType } from '$lib/shared/db';
</script>

<div class="button-container">
  <Button onClick={run}>
    <div class="button">Rerun</div>

    {#snippet loading()}
      <div class="button">
        <LoadingSpinner size="1em" />
      </div>
    {/snippet}
  </Button>
</div>

<style lang="scss">
  div.button-container {
    margin: 8px;
  }

  div.button {
    padding: 8px;
  }
</style>
