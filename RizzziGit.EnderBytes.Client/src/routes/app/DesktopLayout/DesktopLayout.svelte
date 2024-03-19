<script lang="ts" context="module">
  import { getContext } from "svelte";
  import type { Readable } from "svelte/store";

  import { STATE_ROOT } from "$lib/values";
</script>

<script lang="ts">
  import NavigationBar from "./NavigationBar.svelte";
  import type { RootState } from "../../+layout.svelte";

  const rootState = getContext<Readable<RootState>>(STATE_ROOT);
</script>

<div class="panel-container">
  <div class="panel left-panel">
    <NavigationBar></NavigationBar>
  </div>

  <div class="panel right-panel">
    <slot name="layout-slot" />
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

    > div.panel {
      padding: 16px;

      box-sizing: border-box;
    }

    > div.left-panel {
      width: 256px;
      height: 100%;

      background-color: var(--primaryContainer);

      box-shadow: gray 2px 0px 8px;
    }

    > div.right-panel {
      height: 100%;

      flex-grow: 1;
    }
  }
</style>
