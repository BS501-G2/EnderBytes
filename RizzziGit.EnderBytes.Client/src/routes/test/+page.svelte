<script lang="ts">
  import {
    Button,
    LoadingSpinner,
    type ButtonCallback,
    Dialog,
    DialogClass
  } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';
  import { testFunctions } from './test-functions';

  const returnedData: Writable<any> = writable(null);
  const messages: Writable<any[]> = writable([]);
  const error: Writable<Error | null> = writable(null);

  async function onClick(
    callback: (log: (data: any) => void) => any | Promise<any>
  ): Promise<void> {
    try {
      $messages = [];
      $returnedData = await callback((data) => {
        messages.update((value) => {
          value.push(data);
          return value;
        });
      });
    } catch (errorData: any) {
      throw ($error = errorData);
    }
  }

  async function dismissError() {
    $error = null;
  }
</script>

{#snippet button(onClick: ButtonCallback, label: string)}
  <Button {onClick}>
    {label}

    {#snippet loading()}
      <LoadingSpinner size="1em" />
    {/snippet}

    {#snippet container(content)}
      <div class="button">
        {@render content()}
      </div>
    {/snippet}
  </Button>
{/snippet}

<div class="container">
  <h2>Buttons</h2>

  <div class="button-list">
    {#each testFunctions as [label, callback]}
      {@render button(() => onClick(callback), label)}
    {/each}
  </div>

  {#if typeof $returnedData === 'function'}
    {@render $returnedData()}
  {:else}
    <h2>Returned Data</h2>

    <div class="json-data">
      <p>JSON</p>
      <pre>{JSON.stringify($returnedData, undefined, '  ')}</pre>
    </div>
  {/if}
</div>

{#if $error != null}
  <Dialog onDismiss={dismissError} dialogClass={DialogClass.Error}>
    {#snippet head()}
      <h2 class="error-head">{$error!.name}</h2>
    {/snippet}

    {#snippet body()}
      <div class="error-message">
        <b>{$error!.message}</b>
        <pre>
          {$error!.stack}
        </pre>
      </div>
    {/snippet}

    {#snippet actions()}
      <Button onClick={dismissError}><div class="button">OK</div></Button>
    {/snippet}
  </Dialog>
{/if}

<style lang="scss">
  h2.error-head {
    color: var(--error);
  }

  div.error-message {
    color: var(--error);

    min-width: min(720px, 100vw - 64px);
  }

  div.container {
    display: flex;
    flex-direction: column;

    padding: 16px;
    gap: 8px;
  }

  div.button-list {
    display: flex;
    flex-direction: row;
    flex-wrap: wrap;
    min-width: 0px;

    gap: 8px;
  }

  div.json-data {
    background-color: var(--backgroundVariant);
    padding: 8px;

    > p {
      font-weight: lighter;
    }

    > pre {
      padding: 8px;
    }
  }

  div.button {
    padding: 8px;
  }
</style>
