<script lang="ts">
  import type { Client } from "$lib/client/client";
  import { onMount } from "svelte";

  export let client: Client;
  export let file: any;
  export let selectedFileIds: number[] = [];

  onMount(() => {
    selectedFileIds = [file.Id];
  });
</script>

<style lang="scss">
</style>
