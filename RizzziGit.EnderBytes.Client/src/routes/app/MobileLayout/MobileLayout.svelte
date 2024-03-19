<script lang="ts">
    import NavigationBar from "./NavigationBar.svelte";

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
      box-shadow: gray 0px -2px 8px;
    }
  }
</style>
