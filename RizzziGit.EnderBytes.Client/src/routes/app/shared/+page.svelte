<script lang="ts">
  import {
    Awaiter,
    Banner,
    BannerClass,
    Button,
    Title,
    type AwaiterResetFunction
  } from '@rizzzi/svelte-commons';
  import FileBrowser, { type FileBrowserState } from '../file-browser.svelte';
  import { writable, type Writable } from 'svelte/store';
  import { listFileAccess, listSharedFiles, getFile } from '$lib/client/api-functions'

  let refresh: Writable<AwaiterResetFunction<null>> = writable();
  const fileBrowserState: Writable<FileBrowserState> = writable({ isLoading: true });
  const errorStore: Writable<Error | null> = writable(null);
</script>

<Awaiter
  bind:reset={$refresh}
  callback={async (): Promise<void> => {
    $fileBrowserState = { isLoading: true }

    try {
      const fileAccesses = await listSharedFiles()

      $fileBrowserState = {
        isLoading: false,

        file: null,
        access: null,
        pathChain: null,
        files: await Promise.all(fileAccesses.map((fileAccess) => getFile(fileAccess.fileId))),
        title: 'Shared Files'
      }
    }
    catch (error: any) {
      $errorStore = error;
      throw error
    }
  }}
>
  {#snippet error({ error })}
    <Banner bannerClass={BannerClass.Error}>
      <div class="error-banner">
        <p class="message">{error.name}: {error.message}</p>
        <Button onClick={() => $refresh(true)}>
          <p class="retry">Retry</p>
        </Button>
      </div>
    </Banner>
  {/snippet}
</Awaiter>

{#if $errorStore == null}
  <FileBrowser {fileBrowserState} />
{/if}
