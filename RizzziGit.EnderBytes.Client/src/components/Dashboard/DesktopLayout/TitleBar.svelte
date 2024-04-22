<script lang="ts">
  import Account from "./TitleBar/Account.svelte";
  import AppTitle from "./TitleBar/AppTitle.svelte";
  import History from "./TitleBar/History.svelte";
  import Miscellaneous from "./TitleBar/Miscellaneous.svelte";
  import SearchBar from "./TitleBar/SearchBar.svelte";
</script>

<div class="top-bar-container">
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

    min-height: 48px;
    max-height: 48px;

    background-color: var(--primaryContainer);

    > div.top-bar {
      margin-left: env(titlebar-area-x);

      width: env(titlebar-area-width);

      height: 100%;
      padding: 0px 8px 0px 8px;

      box-sizing: border-box;

      display: flex;
      gap: 8px;
    }
  }
</style>
