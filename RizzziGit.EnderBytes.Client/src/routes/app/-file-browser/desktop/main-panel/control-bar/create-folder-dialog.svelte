<script lang="ts" context="module">
</script>

<script lang="ts">
	import { Dialog } from '@rizzzi/svelte-commons';
	import { type FileBrowserState } from '../../../../file-browser.svelte';

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
		<h2>New Folder</h2>
	{/snippet}
	{#snippet body()}{/snippet}
</Dialog>
