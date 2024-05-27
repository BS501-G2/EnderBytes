<script lang="ts">
  import { Button, ButtonClass, Tab, createTabId, type TabItem } from '@rizzzi/svelte-commons';
  import FileOverviewTab from './file-overview-tab.svelte';
  import FileHistoryTab from './file-history-tab.svelte';
  import type { FileResource } from '$lib/client/file';

  const { file }: { file: FileResource } = $props();

  const tabs: TabItem<{
    icon: string;
    description: string;
  }>[] = [
    {
      view: overview,
      name: 'Overview',
      icon: 'fa-solid fa-file-lines',
      description: 'View file details.'
    },
    {
      view: history,
      name: 'History',
      icon: 'fa-solid fa-clock',
      description: 'View file history.'
    },
    {
      view: access,
      name: 'Access',
      icon: 'fa-solid fa-lock',
      description: 'View file access.'
    }
  ];
  const tabId = createTabId(tabs);
</script>

{#snippet overview()}
  <FileOverviewTab {file} />
{/snippet}

{#snippet history()}
  <FileHistoryTab {file} />
{/snippet}

{#snippet access()}
  <p>Access</p>
{/snippet}

<Tab id={tabId}>
  {#snippet container(setTab, content)}
    <div class="tab">
      {@render content()}
    </div>
  {/snippet}

  {#snippet host(tabs, currentIndex, setTab)}
    <div class="tab-host">
      {#each tabs as tab, tabIndex}
        <Button
          buttonClass={ButtonClass.Transparent}
          outline={false}
          onClick={() => setTab(tabIndex)}
        >
          <div class="button{tabIndex == currentIndex ? ' active' : ''}">
            <i class="icon fa-solid {tab.icon}"></i>
            <p>{tab.name}</p>
          </div>
        </Button>
      {/each}
    </div>
  {/snippet}
  {#snippet view(view)}
    <div class="tab-view">
      {@render view()}
    </div>
  {/snippet}
</Tab>

<style lang="scss">
  div.tab {
    display: flex;
    flex-direction: column;
    gap: 8px;
    flex-grow: 1;

    min-height: 0px;
  }

  div.tab-host {
    display: flex;
    flex-direction: row;
    justify-content: safe center;
    gap: 8px;
  }

  div.tab-view {
    display: flex;
    flex-direction: column;
    gap: 8px;

    min-height: 0px;
  }

  div.button {
    padding: 8px;
    border-bottom: 1px solid transparent;
  }

  div.button.active {
    border-bottom-color: var(--onBackgroundVariant);
  }
</style>
