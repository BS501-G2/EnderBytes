<script lang="ts">
  import { goto } from '$app/navigation';
  import { page } from '$app/stores';
  import Locale, { LocaleKey } from '$lib/locale.svelte';
  import {
    Button,
    ButtonClass,
    Tab,
    Title,
    createTabId,
    type TabItem
  } from '@rizzzi/svelte-commons';
  import type { Snippet } from 'svelte';
  import { derived } from 'svelte/store';

  const { children }: { children?: Snippet } = $props();

  const tabs: TabItem<{ path: string }>[] = [
    {
      name: 'Audit Log',
      view,
      path: 'logs'
    },
    {
      name: 'User',
      view,
      path: 'user'
    }
  ];

  const currentTabIndex = derived(page, ($page) =>
    tabs.findIndex((tab) => $page.url.pathname.split('/')?.[2] === tab.path)
  );

  const tabId = createTabId(tabs, $currentTabIndex >= 0 ? $currentTabIndex : 0);

  async function switchTab(index: number): Promise<void> {
    await goto(`/admin/${tabs[index].path}`);
  }
</script>

{#snippet view()}
  {#if children}
    {@render children()}
  {/if}
{/snippet}

<Title title="Admin Page" />

<Tab id={tabId}>
  {#snippet container(_, content)}
    <div class="tab">
      {@render content()}
    </div>
  {/snippet}

  {#snippet view(view)}
    <div class="tab-view">
      {@render view()}
    </div>
  {/snippet}

  {#snippet host(tabs, currentIndex, setTab)}
    <div class="tab-host">
      <p class="site-name"><Locale string={[[LocaleKey.AppName]]} /></p>
      {#each tabs as tab, tabIndex}
        <Button
          buttonClass={ButtonClass.Transparent}
          outline={false}
          onClick={() => switchTab(tabIndex)}
        >
          <div class="tab-button {tabIndex == currentIndex ? 'active' : ''}">
            <p>{tab.name}</p>
          </div>
        </Button>
      {/each}
    </div>
  {/snippet}
</Tab>

<style lang="scss">
  div.tab {
    display: flex;
    flex-direction: column;
    gap: 8px;
    padding: 8px;

    min-height: 100vh;
    box-sizing: border-box;
  }

  div.tab-host {
    display: flex;
    flex-direction: row;
    align-items: center;

    gap: 8px;

    background-color: var(--backgroundVariant);
    color: var(--onBackgroundVariant);

    border-radius: 8px;
    padding: 8px 16px;

    p.site-name {
      font-weight: lighter;
    }
  }

  div.tab-button {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 8px;
    padding: 8px;

    border-bottom: 2px solid transparent;
  }

  div.tab-button.active {
    border-bottom-color: var(--primary);
  }

  div.tab-view {
    flex-grow: 1;

    display: flex;
    flex-direction: row;

    gap: 8px;
  }
</style>
