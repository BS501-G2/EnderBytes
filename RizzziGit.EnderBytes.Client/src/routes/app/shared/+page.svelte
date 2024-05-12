<script lang="ts">
	import Title from '$lib/widgets/title.svelte';
	import FileBrowser, { type FileBrowserState } from '../file-browser.svelte';

	const fileBrowserState: FileBrowserState = {
		isLoading: false,

		files: [],
		pathChain: null,
		access: null,
		file: null,

		allowCreate: false
	};
</script>

<Title title="Shared" />

<FileBrowser {fileBrowserState} />
