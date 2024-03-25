<script lang="ts" context="module">
</script>

<script lang="ts">
  import NavigationBar from "./NavigationBar.svelte";
  import TitleBar from "./TitleBar.svelte";
</script>

<div class="viewport">
  <TitleBar></TitleBar>

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

  }

  div.panel-container {
    flex-grow: 1;

    width: 100%;

    display: flex;

    > div.panel {
      padding: 16px 16px 16px 16px;

      box-sizing: border-box;
    }

    > div.left-panel {
      min-width: 256px;
      height: 100%;

      background-color: var(--primaryContainer);
    }

    > div.right-panel {
      height: 100%;

      flex-grow: 1;
    }
  }
</style>
