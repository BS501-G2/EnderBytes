<script lang="ts" context="module">
  enum ActionTab {
    Operations,
    Notification,
  }
</script>

<script lang="ts">
  import NavigationBar from "./DesktopLayout/NavigationBar.svelte";
  import TitleBar from "./DesktopLayout/TitleBar.svelte";
  import Keyboard from "../Bindings/Keyboard.svelte";
  import { ViewMode, viewMode } from "../Bindings/ResponsiveLayoutRoot.svelte";
</script>

<Keyboard />

<div class="backdrop">
  <div class="viewport">
    <TitleBar />

    <div class="panel-container">
      <div
        class="panel left-panel {!($viewMode & ViewMode.OverlayControls)
          ? 'non-pwa'
          : ''}"
      >
        <NavigationBar />
          <div class="divider"></div>
      </div>

      <div
        class="panel right-panel {!($viewMode & ViewMode.OverlayControls)
          ? 'non-pwa'
          : ''}"
      >
        <slot />
      </div>
    </div>
  </div>
</div>

<style lang="scss">
  div.backdrop {
    min-width: 100vw;
    min-height: 100vh;
    max-width: 100vw;
    max-height: 100vh;

    display: flex;

    position: fixed;
    flex-direction: column;

    // background-image: url("/background.png");
    background-color: var(--primaryContainer);

    left: 0px;
    top: 0px;

    > div.viewport {
      flex-grow: 1;

      min-width: 100vw;
      min-height: 100vh;
      max-width: 100vw;
      max-height: 100vh;

      display: flex;

      position: fixed;
      flex-direction: column;

      backdrop-filter: blur(8px);
    }
  }

  div.panel-container {
    flex-grow: 1;

    display: flex;
    flex-direction: row;

    min-width: 100vw;
    max-width: 100vw;
    min-height: 0px;

    box-sizing: border-box;

    > div.panel {
      box-sizing: border-box;

      min-height: 100%;
      max-height: 100%;

      margin: 8px;
    }

    > div.panel.non-pwa {
      margin-top: 0px;
    }

    > div.left-panel {
      min-width: 256px;
      max-width: 256px;

      display: flex;
      flex-direction: column;

      > div.divider {
        min-height: 1px;
        max-height: 1px;
        background-color: var(--primary);
      }
    }

    > div.right-panel {
      min-width: 0px;
      flex-grow: 1;

      border-radius: 16px;
      overflow: hidden;

      background-color: var(--background);
      color: var(--onBackground);
    }
  }
</style>
