<script lang="ts" context="module">
  import { getContext, onMount } from "svelte";
  import type { Readable } from "svelte/store";

  import { STATE_ROOT } from "$lib/values";
</script>

<script lang="ts">
  import NavigationBar from "./NavigationBar.svelte";
  import type { RootState } from "../../+layout.svelte";
  import TitleBar from "./TitleBar.svelte";

  const rootState = getContext<Readable<RootState>>(STATE_ROOT);

  export let searchString: string
</script>

<div class="viewport">
  <div class="top-bar-container">
    <div class="top-bar">
      <TitleBar bind:searchString={searchString}></TitleBar>
    </div>
  </div>

  <div class="panel-container">
    <div class="panel left-panel">
      <NavigationBar />
    </div>

    <div class="panel right-panel">
      <slot name="layout-slot" />
      <p>Sample Data</p>
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

    > div.top-bar-container {
      min-height: 48px;

      app-region: drag;

      background-color: var(--primaryContainer);

      > div.top-bar {
        margin-left: env(titlebar-area-x);

        width: env(titlebar-area-width);

        height: 100%;

        display: flex;
        flex-direction: row-reverse;
      }
    }

    > div.panel-container {
      flex-grow: 1;

      width: 100%;

      display: flex;

      > div.panel {
        padding: 16px 16px 16px 16px;

        box-sizing: border-box;
      }

      > div.left-panel {
        min-width: 32px;
        height: 100%;

        background-color: var(--primaryContainer);
      }

      > div.right-panel {
        height: 100%;

        flex-grow: 1;
      }
    }
  }
</style>
