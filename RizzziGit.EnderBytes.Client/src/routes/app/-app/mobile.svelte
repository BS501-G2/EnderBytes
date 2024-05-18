<script lang="ts">
    import type { Snippet } from 'svelte';
  import NavigationBar from './mobile/navigation-bar.svelte';

  const { children }: { children: Snippet } = $props()
</script>

<div class="panel-container">
  <div class="panel top-panel"></div>
  <div class="panel middle-panel">
    {@render children()}
  </div>
  <div class="panel bottom-panel">
    <NavigationBar />
  </div>
</div>

<style lang="scss">
  div.panel-container {
    -webkit-app-region: no-drag;

    width: 100vw;
    height: 100vh;

    display: flex;

    position: fixed;
    left: 0px;
    top: 0px;

    flex-direction: column;

    > div.top-panel {
      padding-top: env(titlebar-area-height, 0px);

      min-height: 32px;

      background-color: var(--primaryContainer);
    }

    > div.middle-panel {
      flex-grow: 1;

      overflow-y: auto;
    }

    > div.bottom-panel {
      box-sizing: border-box;

      padding: 8px;

      background-color: var(--primaryContainer);
    }
  }
</style>
