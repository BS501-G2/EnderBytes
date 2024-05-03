<script lang="ts">
	import FileElement from '../../File.svelte';
	import {
		createFile,
		type FileBrowserFolderInformation,
		type FileBrowserSelection
	} from '../../../FileBrowser.svelte';
	import LoadingSpinnerPage from '../../../Widgets/LoadingSpinnerPage.svelte';
	import { hasKeys } from '../../../Bindings/Keyboard.svelte';
	import type { AwaiterResetFunction } from '../../../Bindings/Awaiter.svelte';

	export let selection: FileBrowserSelection;
	export let info: FileBrowserFolderInformation | null;
	export let reset: AwaiterResetFunction;

	$: files = info?.files ?? [];

	let dnd: boolean = false;
	let container: HTMLDivElement;

	let cursorX: number = 0;
	let cursorY: number = 0;

	function updateDndLocation(x: number, y: number) {
		cursorX = x + 32;
		cursorY = y + 32;
	}
</script>

{#if dnd}
	<div class="dnd" style="left: {cursorX}px; top: {cursorY}px; ">
		<p>Drop files here</p>
	</div>
{/if}

<div
	class="file-list-container"
	bind:this={container}
	on:dragleave|preventDefault={() => {
		dnd = false;
	}}
	on:dragover|preventDefault={(event) => {
		dnd = true;
		updateDndLocation(event.clientX, event.clientY);
	}}
	on:drop|preventDefault={async (event) => {
		if (event.dataTransfer == null) {
			return;
		}

		dnd = false;
		const files = Array.from(event.dataTransfer.files);

		if (files.length != 0) {
			await Promise.all(
				Array.from(files).map(async (file) => {
					await createFile(info?.current.id, file);
				})
			);
			await reset();
		}
	}}
	role="region"
>
	<div
		class="file-list"
		role="none"
		on:click={(event) => {
			if (event.target == event.currentTarget) {
				if (hasKeys('shift') || hasKeys('control')) {
					return;
				}

				$selection = [];
			}
		}}
	>
		{#if info == null}
			<LoadingSpinnerPage />
		{:else if files.length === 0}
			<div class="empty-banner">
				<i class="fa-regular fa-folder-open empty-icon"></i>
				<p>This folder seems empty.</p>
			</div>
		{:else}
			{#each files as file, index}
				<FileElement
					{file}
					selected={$selection.includes(file)}
					onClick={() => {
						if (hasKeys('control')) {
							$selection = !$selection.includes(file)
								? [...$selection, file]
								: $selection.filter((id) => id !== file);
						} else if (hasKeys('shift')) {
							if ($selection.length === 0) {
								$selection = [file];
							} else {
								const startIndex = files.indexOf($selection[0]);
								const endIndex = index;

								if (startIndex > endIndex) {
									$selection = files.slice(endIndex, startIndex + 1).toReversed();
								} else {
									$selection = files.slice(startIndex, endIndex + 1);
								}
							}
						} else if ($selection.length !== 1 || $selection[0] !== file) {
							$selection = [file];
						} else {
							$selection = [];
						}
					}}
				/>
			{/each}
		{/if}
	</div>
</div>

<style lang="scss">
	div.file-list-container {
		display: flex;
		flex-direction: column;
		flex-grow: 1;
		align-items: center;
		background-color: var(--backgroundVariant);
		border-radius: 0.5em;

		padding: 8px;
		flex-basis: 0px;

		min-height: 0px;

		> div.file-list {
			flex-grow: 1;

			width: 100%;
			height: 100%;
			box-sizing: border-box;

			padding: 8px;

			display: flex;
			flex-wrap: wrap;
			align-items: center;
			align-content: start;
			// justify-content: start;
			gap: 8px;

			overflow-y: auto;
			overflow-x: hidden;

			> div.empty-banner {
				width: 100%;
				height: 100%;

				display: flex;
				flex-direction: column;
				align-items: center;
				justify-content: center;

				gap: 3em;

				> i.empty-icon {
					font-size: 4em;
					font-weight: 100;
				}
			}
		}
	}

	div.dnd {
		position: fixed;
		background-color: var(--backgroundVariant);
		color: var(--onBackgroundVariant);
		box-shadow: 2px 2px 8px var(--shadow);

		padding: 0.5em;
		border-radius: 0.5em;
		pointer-events: none;
	}
</style>
