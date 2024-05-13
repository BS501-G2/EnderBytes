<script lang="ts" context="module">
</script>

<script lang="ts">
	import Dialog from '$lib/widgets/dialog.svelte';
	import FileBrowser, { type FileBrowserState } from '../../../../file-browser.svelte';

	let {
		fileBrowserState = $bindable(),
		enabled = $bindable()
	}: { fileBrowserState: FileBrowserState; enabled: boolean } = $props();
</script>

<Dialog
	onDismiss={() => {
		enabled = false;
	}}
>
	{#snippet head()}
		<h2>ASd</h2>
	{/snippet}
	{#snippet body()}{/snippet}
</Dialog>
