<script lang="ts">
  import {
    ResponsiveLayout,
    ViewMode,
    viewMode,
    Input,
    InputType,
    Button,
    ButtonClass,
    Dialog,
    DialogClass
  } from '@rizzzi/svelte-commons';
  import { type Writable, writable } from 'svelte/store';
  import { authenticateByPassword } from '$lib/client/api-functions';
  import { type Snippet } from 'svelte';

  const {}: {} = $props();

  const username: Writable<string> = writable('');
  const password: Writable<string> = writable('');

  const errorStore: Writable<Error | null> = writable(null);
</script>

{#snippet buttonContainer(view: Snippet)}
  <div class="button-container">{@render view()}</div>
{/snippet}

{#if $errorStore != null}
  <Dialog
    onDismiss={() => {
      $errorStore = null;
    }}
    dialogClass={DialogClass.Error}
  >
    {#snippet head()}
      <h2>Error</h2>
    {/snippet}
    {#snippet body()}
      {$errorStore!.message}
    {/snippet}
    {#snippet actions()}
      <Button
        onClick={() => {
          $errorStore = null;
        }}
        container={buttonContainer}
      >
        Close
      </Button>
    {/snippet}
  </Dialog>
{/if}

<div class="container{$viewMode & ViewMode.Mobile ? ' mobile' : ''}">
  <ResponsiveLayout>
    {#snippet desktop()}
      <div class="banner"></div>
    {/snippet}
  </ResponsiveLayout>

  <div class="form{$viewMode & ViewMode.Mobile ? ' mobile' : ''}">
    <div class="site-logo">
      <img src="/favicon.svg" alt="logo" />
      <h2>EnderDrive</h2>
    </div>
    <div class="fields">
      <Input type={InputType.Text} name="Username" value={username} />
      <Input type={InputType.Password} name="Password" value={password} />
      <Button
        buttonClass={ButtonClass.Primary}
        onClick={async () => {
          try {
            await authenticateByPassword($username, $password);

          }
          catch (error: any) {
            $errorStore = error;
          }
        }}
        container={buttonContainer}
      >
        Login
      </Button>
    </div>
  </div>
</div>

<style lang="scss">
  :global(body) {
    display: flex;
    flex-direction: row;
    justify-content: safe center;
  }

  div.container {
    max-width: 1280px;
    min-width: 0px;

    flex-grow: 1;
    display: flex;
    flex-direction: row;

    align-items: stretch;

    padding: 16px;
    gap: 16px;

    > div.banner,
    > div.form {
      background-color: var(--backgroundVariant);
      color: var(--onBackgroundVariant);

      padding: 16px;
      border-radius: 16px;
      box-sizing: border-box;
    }

    > div.banner {
      flex-grow: 1;
    }

    > div.form {
      min-width: 320px;
      max-width: 320px;

      display: flex;
      flex-direction: column;
      gap: 8px;

      justify-content: safe center;

      > div.fields {
        display: flex;
        flex-direction: column;
        gap: 16px;
      }

      > div.site-logo {
        display: flex;
        flex-direction: row;

        gap: 16px;
        padding: 16px;

        justify-content: safe center;
        align-items: center;

        > h2 {
          font-weight: lighter;
        }

        > img {
          width: 64px;
          height: 64px;
        }
      }
    }

    > div.form.mobile {
      min-width: 0px;
      max-width: unset;
      flex-grow: 1;
    }
  }

  div.container.mobile {
    padding: unset;

    > div.form {
      border-radius: 0px;
    }
  }

  div.button-container {
    padding: 8px;
  }
</style>
