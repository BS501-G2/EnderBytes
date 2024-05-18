<script lang="ts">
  import type { FileBrowserState } from '../file-browser.svelte';

  let { fileBrowserState = $bindable() }: { fileBrowserState: FileBrowserState } = $props();
</script>

<div class="main">
  <div class="main panel"></div>
</div>

<style lang="scss">

</style>
