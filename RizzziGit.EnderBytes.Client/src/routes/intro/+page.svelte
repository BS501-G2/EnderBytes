<script lang="ts">
  import { goto } from "$app/navigation";
  import { onMount } from "svelte";

  onMount(() => goto("/intro/home", { replaceState: true }));
</script>
