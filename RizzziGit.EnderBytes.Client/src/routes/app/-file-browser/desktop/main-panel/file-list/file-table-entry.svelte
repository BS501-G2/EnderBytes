<script lang="ts">
	import { goto } from '$app/navigation';
	import type { FileResource, FileBrowserState } from '../../../../file-browser.svelte';

	let {
		file,
		fileBrowserState,
		onClick,
		selection = $bindable()
	}: {
		fileBrowserState: FileBrowserState & { isLoading: false };
		file: FileResource;
		onClick: (fileBrowserState: FileBrowserState & { isLoading: false }, file: FileResource) => void;
		selection: FileResource[];
	} = $props();
</script>
