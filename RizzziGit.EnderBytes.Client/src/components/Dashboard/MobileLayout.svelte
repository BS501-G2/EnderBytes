<script lang="ts">
    import type { Writable } from "svelte/store";
    import NavigationBar from "./MobileLayout/NavigationBar.svelte";
    import type { AppState } from "$lib/states/app-state";
</script>

<div class="panel-container">
  <div class="panel top-panel">
    <slot name="layout-slot" />
  </div>

  <div class="panel bottom-panel">
    <NavigationBar></NavigationBar>
  </div>
</div>

<style lang="scss">
  div.panel-container {
    width: 100vw;
    height: 100vh;

    display: flex;

    position: fixed;
    left: 0px;
    top: 0px;

    flex-direction: column;

    > div.top-panel {
      flex-grow: 1;

      overflow-y: auto;
    }

    > div.bottom-panel {
      min-height: 32px;

      background-color: var(--primaryContainer);
    }
  }
</style>
