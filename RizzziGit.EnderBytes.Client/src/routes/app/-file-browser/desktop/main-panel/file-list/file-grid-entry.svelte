<script lang="ts">
	import { goto } from '$app/navigation';
	import type { FileResource, FileBrowserState } from '../../../../file-browser.svelte';

	let {
		file,
		fileBrowserState = $bindable(),
		onClick,
		selection = $bindable()
	}: {
		fileBrowserState: FileBrowserState & { isLoading: false };
		file: FileResource;
		onClick: (
			fileBrowserState: FileBrowserState & { isLoading: false },
			file: FileResource
		) => void;
		selection: FileResource[];
	} = $props();
</script>

<button
	class="file-entry {selection.includes(file) ? 'selected' : ''}"
	onclick={(event) => {
		event.preventDefault();

		if (fileBrowserState.isLoading) {
			return;
		}

		onClick(fileBrowserState, file);
	}}
	ondblclick={(event) => {
		event.preventDefault();

		goto(`/app/files?id=${file.id}`);
	}}
>
	<img class="thumbnail" alt="Thumbnail" />

	<p class="name">
		<i class="fa-regular {file.isFolder ? 'fa-folder' : 'fa-file'}"></i>
		<span class="name">
			{file.name}
		</span>
	</p>
</button>

<style lang="scss">
	button.file-entry {
		cursor: pointer;

		display: flex;
		flex-direction: column;

		padding: 8px;
		gap: 8px;

		background-color: var(--background);
		color: var(--onBackground);

		border: 1px solid transparent;
		border-radius: 0.5em;

		text-decoration: none;

		> img.thumbnail {
			min-width: 100%;
			max-width: 100%;
			aspect-ratio: 5/4;

			object-fit: contain;
			border-radius: 0.25em;

			background-color: var(--backgroundVariant);
		}

		> p.name {
			min-width: 100%;
			max-width: 100%;

			display: flex;
			flex-direction: row;
			align-items: center;

			gap: 8px;

			> span.name {
				text-overflow: ellipsis;
				text-wrap: nowrap;
				overflow: hidden;
				line-height: 1em;
			}
		}
	}

	button.file-entry:hover {
		border: 1px solid var(--onBackgroundVariant);
	}

	button.file-entry.selected {
		background-color: var(--primary);
		color: var(--onPrimary);
	}
</style>
