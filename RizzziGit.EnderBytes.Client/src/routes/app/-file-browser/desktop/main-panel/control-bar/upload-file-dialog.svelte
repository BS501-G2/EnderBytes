<script lang="ts" context="module">
</script>

<script lang="ts">
	import Dialog from '$lib/widgets/dialog.svelte';

	const {}: {} = $props();
</script>

<Dialog onDismiss={() => {}}>
	{#snippet head()}
		<h2>ASd</h2>
	{/snippet}
	{#snippet body()}{/snippet}
</Dialog>
