<script lang="ts" context="module">
</script>

<script lang="ts">
    import { RootState } from "$lib/states/root-state";

  import NavigationBar from "./Navigation.svelte";
  import TitleBar from "./TitleBar.svelte";

  const rootState = RootState.state
  const appState = $rootState.appState
  const fileState = $appState.fileState
</script>

<div class="viewport">
  <TitleBar></TitleBar>

  <div class="panel-container">
    <div class="panel left-panel">
      <NavigationBar />
    </div>

    <div class="panel right-panel">
      <slot name="layout-slot" />
    </div>
  </div>
</div>

<style lang="scss">
  div.viewport {
    width: 100vw;
    height: 100vh;

    display: flex;

    position: fixed;
    flex-direction: column;

    left: 0px;
    top: 0px;
  }

  div.panel-container {
    flex-grow: 1;

    width: 100%;

    display: flex;

    > div.panel {
      box-sizing: border-box;
    }

    > div.left-panel {
      padding: 16px;

      min-width: 256px;
      height: 100%;

      background-color: var(--primaryContainer);

      display: flex;
      flex-direction: column;
    }

    > div.right-panel {
      height: 100%;

      flex-grow: 1;
    }
  }
</style>
