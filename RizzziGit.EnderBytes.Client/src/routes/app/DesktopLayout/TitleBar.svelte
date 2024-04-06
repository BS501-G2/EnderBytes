<script lang="ts">
  import ActionChip from "./TitleBar/ActionChip.svelte";
import AppTitle from "./TitleBar/AppTitle.svelte";
  import SearchBar from "./TitleBar/SearchBar.svelte";
</script>

<div class="top-bar-container">
  <div class="top-bar">
    <AppTitle></AppTitle>
    <SearchBar></SearchBar>
    <ActionChip></ActionChip>
  </div>
</div>

<style lang="scss">
  div.top-bar-container {
    -webkit-app-region: drag;

    min-height: 48px;

    background-color: var(--primaryContainer);

    > div.top-bar {
      margin-left: env(titlebar-area-x);

      width: env(titlebar-area-width);

      height: 100%;

      display: flex;
    }
  }
</style>
