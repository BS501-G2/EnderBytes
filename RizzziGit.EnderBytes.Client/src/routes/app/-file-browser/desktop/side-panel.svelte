<script lang="ts">
  import { type Writable, type Readable, derived, writable } from 'svelte/store';
  import type {
    FileAccessResource,
    FileBrowserState,
    FileResource
  } from '../../file-browser.svelte';
  import { Awaiter, Button, ButtonClass, LoadingSpinner } from '@rizzzi/svelte-commons';
  import { apiFetch } from '$lib/client.svelte';
  import UserName from '$lib/widgets/user-name.svelte';

  let {
    fileBrowserState,
    selection
  }: { fileBrowserState: Writable<FileBrowserState>; selection: Writable<FileResource[]> } =
    $props();

  const selected: Readable<FileResource | null> = derived(
    [fileBrowserState, selection],
    ([fileBrowserState, selection]) =>
      selection.length === 1
        ? selection[0]
        : fileBrowserState.isLoading
          ? null
          : fileBrowserState.file
  );

  type Tab = [name: string, icon: string, description: string];
  const tabs: Tab[] = [
    ['Details', 'fa-solid fa-file-lines', 'View file details.'],
    ['History', 'fa-solid fa-clock', 'View file history.'],
    ['Access', 'fa-solid fa-lock', 'View file access.']
  ];

  const currentTab: Writable<Tab> = writable(tabs[0]);
</script>

{#snippet detailsTab(file: FileResource, fileAccess: FileAccessResource | null)}
  <div class="thumbnail">
    <img alt="Thumbnail" class="thumbnail" />
  </div>
  <div class="details-table">
    <div class="row">
      <p class="details-name">Name</p>
      <p class="details-value">{file.name}</p>
    </div>
    <div class="row">
      <p class="details-name">Created On</p>
      <p class="details-value">{new Date(file.createTime).toLocaleString()}</p>
    </div>
    {#if file.createTime != file.updateTime}
      <div class="row">
        <p class="details-name">Modified On</p>
        <p class="details-value">{new Date(file.updateTime).toLocaleString()}</p>
      </div>
    {/if}
    <div class="row">
      <p class="details-name">Created By</p>
      <p class="details-value">
        {#key file.domainUserId}
          <Awaiter callback={() => apiFetch({ path: `/user/:${file.authorUserId}` })}>
            {#snippet success({ result })}
              <UserName user={result} />
            {/snippet}
            {#snippet loading()}
              <LoadingSpinner size="1em" />
            {/snippet}
          </Awaiter>
        {/key}
      </p>
    </div>
    {#if file.domainUserId != file.authorUserId}
      <div class="row">
        <p class="details-name">Owned By</p>
        <p class="details-value">
          {#key file.domainUserId}
            <Awaiter callback={() => apiFetch({ path: `/user/:${file.domainUserId}` })}>
              {#snippet success({ result })}
                <UserName user={result} />
              {/snippet}
              {#snippet loading()}
                <LoadingSpinner size="1em" />
              {/snippet}
            </Awaiter>
          {/key}
        </p>
      </div>
    {/if}
    {#if !file.isFolder}
      <div class="row">
        <p class="details-name">Content</p>
        <p class="details-value">
          {#key file.id}
            <Awaiter
              callback={async () => {
                const fileContentMain = await apiFetch({
                  path: `/file/:${file.id}/content/!main`
                });

                const fileContentVersions = await apiFetch({
                  path: `/file/:${file.id}/content/:${fileContentMain.id}/version`
                })

                console.log(fileContentVersions);
                return fileContentMain;
              }}
            >
              {#snippet success({ result })}
                {JSON.stringify(result, undefined, '  ')}
              {/snippet}
              {#snippet loading()}
                <LoadingSpinner size="1em" />
              {/snippet}
            </Awaiter>
          {/key}
        </p>
      </div>
    {/if}
  </div>
{/snippet}

{#snippet historyTab(file: FileResource)}{/snippet}

{#snippet accessTab(file: FileResource, fileAccess: FileAccessResource | null)}{/snippet}

{#snippet tabView(file: FileResource, fileAccess: FileAccessResource | null)}
  <div class="tab-view">
    {#if $currentTab == tabs[0]}
      {@render detailsTab(file, fileAccess)}
    {:else if $currentTab == tabs[1]}
      {@render historyTab(file)}
    {:else if $currentTab == tabs[2]}
      {@render accessTab(file, fileAccess)}
    {/if}
  </div>
{/snippet}

{#snippet tabHost()}
  <div class="tab-host">
    {#each tabs as tab}
      <Button
        outline={false}
        onClick={() => {
          $currentTab = tab;
        }}
        buttonClass={ButtonClass.Transparent}
      >
        <div class="tab-button{tab[0] == $currentTab[0] ? ' active' : ''}">
          <i class="tab-icon {tab[1]}"></i>
          <p>{tab[0]}</p>
        </div>
      </Button>
    {/each}
  </div>
{/snippet}

{#snippet sidePanel(selected: FileResource | null, fileAccess: FileAccessResource | null, size: number)}
  <div class="header">
    <i class="icon fa-solid fa-{selected?.isFolder ? 'folder' : 'file'}"></i>
    <h3 class="file-name">
      {#if size > 1}
        {size} selected
      {:else if selected != null && selected.parentId != null}
        {selected.name}
      {:else if $fileBrowserState.title != null}
        {$fileBrowserState.title}
      {/if}
    </h3>
  </div>
  {#if selected != null && size == 1}
    <div class="body">
      {@render tabHost()}
      {@render tabView(selected, fileAccess)}
    </div>
  {/if}
{/snippet}

<div class="side-panel-container">
  {#if !$fileBrowserState.isLoading}
    {@render sidePanel(
      $selected,
      $selected == $fileBrowserState.access ? $fileBrowserState.access : null,
      $selection.length || 1
    )}
  {/if}
</div>

<style lang="scss">
  div.tab-host {
    display: flex;
    flex-direction: row;
    align-items: center;
    border-bottom: 2px solid transparent;

    justify-content: space-evenly;
  }

  div.tab-button {
    border-bottom: 2px solid transparent;
    padding: 8px;
  }

  div.tab-button.active {
    border-bottom: 2px solid var(--primary);
  }

  div.body {
    flex-grow: 1;
    min-height: 0px;
    min-width: 0px;

    display: flex;
    flex-direction: column;

    gap: 8px;
    padding: 8px;
  }

  div.header {
    display: flex;
    flex-direction: row;
    align-items: center;

    gap: 8px;

    > h3.file-name {
      flex-grow: 1;

      overflow: hidden;
      text-overflow: ellipsis;
      text-wrap: nowrap;
    }

    > i.icon {
      min-width: 16px;

      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
    }
  }

  div.tab-view {
    flex-grow: 1;
    display: flex;
    flex-direction: column;

    overflow: auto;

    gap: 8px;
  }

  div.side-panel-container {
    border-radius: 8px;

    display: flex;
    flex-direction: column;
    gap: 8px;

    min-width: 320px;
    max-width: 320px;

    padding: 16px;
    box-sizing: border-box;

    background-color: var(--backgroundVariant);
  }

  div.thumbnail {
    min-width: 100%;
    max-width: 100%;

    aspect-ratio: 5/4;

    padding: 8px;

    border: 1px solid var(--shadow);
    box-sizing: border-box;

    > img {
      min-width: 100%;
      max-width: 100%;
      min-height: 100%;
      max-height: 100%;

      box-sizing: border-box;
      aspect-ratio: 5/4;
    }
  }

  div.details-table {
    display: flex;
    flex-direction: column;
    gap: 8px;

    > div.row {
      display: flex;
      flex-direction: row;
      gap: 8px;

      > p {
        min-width: 0px;
        max-width: 100%;
        flex-grow: 1;

        overflow: hidden;
        text-overflow: ellipsis;
        text-wrap: nowrap;
      }

      > p.details-name {
        font-weight: bolder;

        text-align: start;
        min-width: min-content;
      }

      > p.details-name::after {
        content: ':';
      }

      > p.details-value {
        text-align: end;
      }
    }
  }
</style>
