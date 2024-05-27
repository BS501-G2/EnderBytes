<script lang="ts" context="module">
  export interface SettingsDialogState {}

  export const settingsDialogState: Writable<SettingsDialogState | null> = writable(null);

  export function hideSettingsDialog() {
    settingsDialogState.set(null);
  }

  export function showSettingsDialog() {
    settingsDialogState.set({});
  }
</script>

<script lang="ts">
  import Locale, {
    LocaleKey,
    LocaleType as LocaleType,
    getLocale,
    setLocale
  } from '$lib/locale.svelte';

  import {
    Banner,
    BannerClass,
    Button,
    ButtonClass,
    Dialog,
    ResponsiveLayout,
    Tab,
    ViewMode,
    createTabId,
    currentColorScheme,
    viewMode,
    colors,
    setColorScheme,
    type ColorSchemeName,
    type TabItem
  } from '@rizzzi/svelte-commons';
  import { writable, type Writable } from 'svelte/store';

  function getLanguageList(): LocaleType[] {
    return [LocaleType.en_US, LocaleType.tl_PH];
  }

  type SettingsTabItem = TabItem<{ description: string; icon: string }>;

  const tabs: SettingsTabItem[] = [
    { name: 'App', description: 'App Settings', icon: 'fas fa-cog', view: appView },
    { name: 'Account', description: 'Account Settings', icon: 'fas fa-user', view: accountView }
  ];
  const tabId = createTabId(tabs);

  const appSettingsChanged: Writable<boolean> = writable(false);
</script>

{#snippet accountView()}{/snippet}

{#snippet appView()}
  {#if $appSettingsChanged}
    <Banner bannerClass={BannerClass.Info}>
      <p>Updated settings will take full effect on reload.</p>
    </Banner>
  {/if}

  <div class="settings-row">
    <p class="label">Language</p>
    <select
      class="value"
      value={getLocale()}
      onchange={({ currentTarget }) => {
        setLocale(currentTarget.value as LocaleType)
        $appSettingsChanged = true
      }}
    >
      {#each getLanguageList() as language}
        <option value={language}>
          <Locale locale={language} string={[[LocaleKey.LanguageName]]}></Locale>
        </option>
      {/each}
    </select>
  </div>
  <div class="settings-row">
    <p class="label">Color Scheme</p>
    <select
      class="value"
      value={$currentColorScheme}
      onchange={({ currentTarget }) => {
        setColorScheme(currentTarget.value as ColorSchemeName)
        $appSettingsChanged = true
      }}
    >
      {#each Object.keys(colors) as key}
        <option value={key}>{key}</option>
      {/each}
    </select>
  </div>
{/snippet}

{#snippet host(tabs: SettingsTabItem[], currentTabIndex:  number, setTab: (index: number) => void)}
  <div class="tab-host">
    {#each tabs as tab, tabIndex}
      <Button
        buttonClass={ButtonClass.Transparent}
        outline={false}
        onClick={() => setTab(tabIndex)}
      >
        <div class="tab-button{tabIndex == currentTabIndex ? ' active' : ''}">
          <i class="icon fa-solid {tab.icon}"></i>
          <p>{tab.name}</p>
        </div>
      </Button>
    {/each}
  </div>
{/snippet}

{#if $settingsDialogState != null}
  <Dialog onDismiss={hideSettingsDialog}>
    {#snippet head()}
      <div class="head">
        <h2>Settings</h2>

        <ResponsiveLayout>
          {#snippet desktop()}
            <Tab id={tabId} {host}>
              {#snippet view()}{/snippet}
            </Tab>
          {/snippet}
        </ResponsiveLayout>
      </div>
    {/snippet}
    {#snippet body()}
      <div class="dialog{$viewMode & ViewMode.Desktop ? ' desktop' : ''}">
        <Tab id={tabId} host={$viewMode & ViewMode.Mobile ? host : undefined}>
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
        </Tab>
      </div>
    {/snippet}
  </Dialog>
{/if}

<style lang="scss">
  div.head {
    display: flex;
    flex-direction: row;
    align-items: center;
    gap: 8px;
  }

  div.dialog {
    display: flex;
    flex-direction: column;
    gap: 8px;
  }

  div.dialog.desktop {
    min-width: min(512px, 100vw - 64px);
  }

  div.tab-host {
    display: flex;
    flex-direction: row;
    align-items: center;
    height: 40px;
  }

  div.tab-view {
    flex-grow: 1;
  }

  div.tab-button {
    display: flex;
    flex-direction: row;
    gap: 8px;
    align-items: center;

    padding: 8px;
    border-bottom: 2px solid transparent;
  }

  div.tab-button.active {
    border-bottom-color: var(--primary);
  }

  div.settings-row {
    display: flex;
    flex-direction: row;
    align-items: center;

    gap: 8px;
    padding: 8px;

    > p.label {
      flex-grow: 1;
    }

    > select.value {
      flex-grow: 1;
      max-width: 128px;

      padding: 8px;

      overflow: hidden;
      text-overflow: ellipsis;
      text-wrap: nowrap;
    }
  }
</style>
