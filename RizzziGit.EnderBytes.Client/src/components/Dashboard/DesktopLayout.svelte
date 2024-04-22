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
</script>

<Keyboard />

<div class="viewport">
  <TitleBar></TitleBar>

  <div class="panel-container">
    <div class="panel left-panel">
      <NavigationBar />
      <div class="divider" />
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

    background-color: var(--primaryContainer);

    > div.panel {
      box-sizing: border-box;
    }

    > div.left-panel {
      min-width: 256px;
      max-width: 256px;
      height: 100%;

      display: flex;
      flex-direction: column;

      min-height: 0px;

      padding: 0px 16px 0px 16px;

      > div.divider {
        min-height: 1px;
        max-height: 1px;
        background-color: var(--primary);
      }
    }

    > div.right-panel {
      flex-grow: 1;

      min-width: 0px;

      border-radius: 16px;

      margin: 0px 8px 8px 8px;

      background-color: var(--background);
      color: var(--onBackground);
    }
  }
</style>
