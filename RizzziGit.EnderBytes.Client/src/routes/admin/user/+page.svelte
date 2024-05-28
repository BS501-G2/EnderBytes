<script lang="ts" context="module">
</script>

<script lang="ts">
  import Page from '../+page.svelte';
</script>

<Page>
  <div class="panel side-panel">
    
  </div>
  <div class="panel main-panel"></div>
</Page>

<style lang="scss">
  div.panel {
    background-color: var(--backgroundVariant);
    color: var(--onBackgroundVariant);

    border-radius: 8px;
  }

  div.main-panel {
    flex-grow: 1;
  }

  div.side-panel {
    min-width: 256px;
    max-width: 256px;
  }
</style>
