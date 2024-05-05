<script lang="ts">
	import { page } from '$app/stores';

	import FileBrowser from '../../../components/FileBrowser.svelte';

	function getCurrentId(url: URL) {
		const currentId = url.pathname.split('/')[3] ?? null;

		return (currentId != null ? Number.parseInt(currentId) : null) ?? null;
	}

	$: currentFileId = getCurrentId($page.url);
</script>

<FileBrowser {currentFileId} />
