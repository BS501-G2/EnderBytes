<script lang="ts">
  import { ViewMode, viewMode } from '@rizzzi/svelte-commons';
  import Account from './title-bar/account.svelte';
  import AppTitle from './title-bar/app-title.svelte';
  import History from './title-bar/history.svelte';
  import Miscellaneous from './title-bar/miscellaneous.svelte';
  import SearchBar from './title-bar/search-bar.svelte';

</script>

<div class="top-bar-container {$viewMode & ViewMode.OverlayControls ? 'pwa' : ''}">
  <div class="top-bar">
    <History />
    <AppTitle />
    <SearchBar />
    <Miscellaneous />
    <Account />
  </div>
</div>

<style lang="scss">
  div.top-bar-container {
    -webkit-app-region: drag;

    // min-height: 48px;
    // max-height: 48px;

    // background-color: var(--background);
    // color: var(--onBackground);

    > div.top-bar {
      margin-left: env(titlebar-area-x);

      min-width: calc(env(titlebar-area-width) - 8px);
      max-width: calc(env(titlebar-area-width) - 8px);

      min-height: 100%;
      max-height: 100%;

      box-sizing: border-box;

      display: flex;
      gap: 8px;
    }
  }

  div.top-bar-container.pwa {
    background-color: var(--primaryContainer);
    color: var(--onPrimaryContainer);
  }
</style>
