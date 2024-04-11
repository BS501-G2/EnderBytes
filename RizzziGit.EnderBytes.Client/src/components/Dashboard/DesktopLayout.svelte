<script lang="ts" context="module">
</script>

<script lang="ts">
  import { RootState } from "$lib/states/root-state";

  import LeftPanelNavigationBar from "./DesktopLayout/LeftPanelNavigationBar.svelte";
  import LeftPanelAccountBar from "./DesktopLayout/LeftPanelAccountBar.svelte";
  import TitleBar from "./DesktopLayout/TitleBar.svelte";
  import Keyboard from "../Keyboard.svelte";
  import type { Client } from "$lib/client/client";

  export let client: Client;

  const rootState = RootState.state;
</script>

<Keyboard />

<div class="viewport">
  <TitleBar></TitleBar>

  <div class="panel-container">
    <div class="panel left-panel">
      <LeftPanelNavigationBar />
      <div class="divider"></div>
      <LeftPanelAccountBar {client} />
    </div>

    <div class="panel right-panel">
      <slot />
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
    min-height: 0px;

    > div.panel {
      box-sizing: border-box;
    }

    > div.left-panel {
      min-width: 256px;
      max-width: 256px;
      height: 100%;

      background-color: var(--primaryContainer);

      display: flex;
      flex-direction: column;

      padding: 0px 16px 0px 16px;

      > div.divider {
        height: 1px;
        background-color: var(--primary);
      }
    }

    > div.right-panel {
      height: 100%;

      flex-grow: 1;

      min-width: 0px;
    }
  }
</style>
