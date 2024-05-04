<script lang="ts">
	import { goto } from '$app/navigation';
	import { FileIcon, FolderIcon } from 'svelte-feather-icons';
	import { hasKeys } from '../Bindings/Keyboard.svelte';

	export let file: any;
	export let selected: boolean = false;

	export let onClick: () => void;

	let hovered: boolean = false;

	let button: HTMLElement;
	let link: HTMLAnchorElement;
</script>

<button
	bind:this={button}
	class="file-entry {selected ? 'selected' : ''}"
	on:pointerenter={() => (hovered = true)}
	on:pointerleave={() => (hovered = false)}
	on:click={onClick}
	on:dblclick={() => {
		const path = '/app/files/' + file.id;

		if (hasKeys('control')) {
			window.open(path, '_blank');
		} else {
			goto(path);
		}
	}}
>
	<div class="preview"></div>
	<div class="top">
		<div class="icon">
			{#if file.isFolder}
				<FolderIcon size="100%" />
			{:else}
				<FileIcon size="100%" />
			{/if}
		</div>
		<div class="name">
			<p>{file.name}</p>
		</div>
	</div>
</button>

<style lang="scss">
	button.file-entry:hover {
		text-decoration: underline;
	}

	button.file-entry {
		background-color: var(--background);
		color: var(--onBackground);
		border: solid 1px transparent;
		cursor: pointer;

		box-sizing: border-box;

		display: flex;
		flex-direction: column;

		border-radius: 8px;
		padding: 8px;
		gap: 8px;

		min-width: 172px;
		max-width: 172px;
		aspect-ratio: 1 / 1;

		> div.preview {
			min-width: 100%;
			max-width: 100%;

			background-color: var(--backgroundVariant);
			color: var(--onBackgroundVariant);

			border-radius: 4px;
		}

		> div.top {
			min-width: 100%;
			max-width: 100%;
			min-height: 1.666em;
			max-height: 1.666em;

			display: flex;
			flex-direction: row;
			align-items: center;

			padding: 0px 4px 0px 4px;

			gap: 8px;

			> div.icon {
				min-width: 16px;
				max-width: 16px;
				aspect-ratio: 1;

				display: flex;
				justify-content: center;
				align-items: center;
			}

			> div.name {
				flex-grow: 1;
				min-width: 0px;

				overflow: hidden;

				> p {
					flex-grow: 1;

					text-wrap: nowrap;
					text-overflow: ellipsis;
					overflow: hidden;

					text-align: start;
					line-height: 1em;
				}
			}
		}

		> div.preview {
			flex-grow: 1;
		}
	}

	button.file-entry.selected {
		border-color: var(--primaryContainer);
		background-color: var(--primary);
		color: var(--onPrimary);
	}

	button.file-entry:hover {
		border-color: var(--primary);
	}
</style>
