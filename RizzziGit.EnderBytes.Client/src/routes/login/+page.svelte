<script lang="ts">
	import { loadClient } from "$lib";

	import LoadingPage from "../../components/loading-page.svelte";
</script>

{#await loadClient()}
  <LoadingPage />
{:then client}
{/await}
