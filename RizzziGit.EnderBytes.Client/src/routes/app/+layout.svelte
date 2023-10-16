<script lang="ts">
	import type { Client } from "$lib/core/client";
	import { onMount, setContext } from "svelte";

  import LoadingPage from "../../components/loading-page.svelte";

  let loadPromise: Promise<Client>

  onMount(async () => {
  })
</script>

{#await loadPromise}
  <LoadingPage />
{:then}
  <slot />
{/await}