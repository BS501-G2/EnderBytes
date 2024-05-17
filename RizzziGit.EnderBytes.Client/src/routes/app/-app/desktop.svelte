<script lang="ts" context="module">
  enum ActionTab {
    Operations,
    Notification
  }
</script>

<script lang="ts">
  import NavigationBar from './desktop/navigation-bar.svelte';
  import TitleBar from './desktop/title-bar.svelte';

  import { Keyboard, ViewMode, viewMode } from '@rizzzi/svelte-commons';
</script>

<Keyboard />

<div class="backdrop">
  <div class="viewport">
    <TitleBar />

    <div class="panel-container">
      <div class="panel left-panel {!($viewMode & ViewMode.OverlayControls) ? 'non-pwa' : ''}">
        <NavigationBar />
      </div>

      <div class="panel right-panel {!($viewMode & ViewMode.OverlayControls) ? 'non-pwa' : ''}">
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
      padding: 8px;
      box-sizing: border-box;
      gap: 8px;

      position: fixed;
      flex-direction: column;

      backdrop-filter: blur(8px);
    }
  }

  div.panel-container {
    flex-grow: 1;

    display: flex;
    flex-direction: row;

    min-height: 0px;

    box-sizing: border-box;

    gap: 8px;

    > div.panel {
      box-sizing: border-box;

      min-height: 0px;
    }

    > div.panel.non-pwa {
      margin-top: 0px;
    }

    > div.left-panel {
      display: flex;
      flex-direction: column;
    }

    > div.right-panel {
      min-width: 0px;
      flex-grow: 1;

      -webkit-app-region: no-drag;

      border-radius: 16px;
      overflow: hidden;

      display: flex;
      flex-direction: column;

      background-color: var(--background);
      color: var(--onBackground);
    }
  }
</style>
