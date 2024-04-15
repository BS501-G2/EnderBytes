<script lang="ts">
  import { onMount } from "svelte";

  export let file: any;
  export let selectedFileIds: number[] = [];

  onMount(() => {
    selectedFileIds = [file.Id];
  });
</script>

<style lang="scss">
</style>
