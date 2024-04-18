<script lang="ts">
  import { onMount } from "svelte";

  export let file: any;
  export let selectedFiles: any[];

  onMount(() => {
    selectedFiles = [file];
  });
</script>

<style lang="scss">
</style>
