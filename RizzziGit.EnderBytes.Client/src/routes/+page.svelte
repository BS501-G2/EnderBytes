<script lang="ts">
  import { goto } from "$app/navigation";
  import { onMount } from "svelte";

  onMount(() => goto("/landing", { replaceState: true }));
</script>
