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
	import { onMount } from 'svelte';
	import { apiFetch } from '../../../Bindings/Client.svelte';
	import { page } from '$app/stores';
	import AnimationFrame from '../../../Bindings/AnimationFrame.svelte';

	export let selection: FileBrowserSelection;
	export let info: FileBrowserFolderInformation | null;
	export let reset: AwaiterResetFunction;

	let files: any[] = [];
	let isBusy: boolean = false;

	const load = (() => {
		let isRunning = false;

		return async function checkPagination(): Promise<void> {
			if (isRunning) {
				return;
			}

			isBusy = isRunning = true;
			try {
				if (info == null) {
					return;
				}

				const response = await apiFetch({
					path: `/file/:${info.current.id}/files`,
					params: (() => {
						const params: Record<string, string> = {};

						const source = $page.url.searchParams;

						for (const name of source.keys()) {
							params[name] = source.get(name) ?? '';
						}

						return params;
					})()
				});

				files = response;
			} finally {
				isBusy = isRunning = false;
			}
		};
	})();

	let dnd: boolean = false;
	let container: HTMLDivElement;
	let listElement: HTMLDivElement;

	let cursorX: number = 0;
	let cursorY: number = 0;

	function updateDndLocation(x: number, y: number) {
		cursorX = x + 32;
		cursorY = y + 32;
	}

	type MouseDragRectangle = [
		basisX: number,
		basisY: number,
		width: number,
		height: number,
		mouseX: number,
		mouseY: number,
		previousSelectionSnapshot: any[]
	];

	let mouseDrag: MouseDragRectangle | null = null;
	let mouseDragElement: HTMLDivElement | null = null;

	function updateBounds(clientX: number, clientY: number) {
		if (mouseDrag == null) {
			return;
		}

		mouseDrag = [
			mouseDrag[0],
			mouseDrag[1],
			clientX - mouseDrag[0],
			clientY - mouseDrag[1] + listElement.scrollTop,
			clientX,
			clientY + listElement.scrollTop,
			mouseDrag[6]
		];
	}

	function updateSelectedByBounds() {
		if (mouseDragElement == null || mouseDrag == null) {
			return;
		}

		const dragArea = mouseDragElement.getBoundingClientRect();
		$selection = mouseDrag[6];

		for (let i = 0; i < files.length; i++) {
			const file = files[i];
			const fileElement = listElement.children[i];

			const fileArea = fileElement.getBoundingClientRect();
			if (
				fileArea.x < dragArea.x + dragArea.width &&
				fileArea.x + fileArea.width > dragArea.x &&
				fileArea.y < dragArea.y + dragArea.height &&
				fileArea.y + fileArea.height > dragArea.y
			) {
				if (hasKeys('shift')) {
					$selection = [...$selection, file];
				} else if (hasKeys('control') && $selection.includes(file)) {
					$selection = $selection.filter((id) => id !== file);
				} else if (!$selection.includes(file)) {
					$selection = [...$selection, file];
				}
			}
		}
	}

	function listenBackgroundEvent(type: 'move' | 'up' | 'down', event: MouseEvent) {
		void (() => {
			if (type === 'up') {
				mouseDrag = null;
			} else if (type === 'down') {
				if (event.target != event.currentTarget) {
					return;
				}

				if (!(hasKeys('shift') || hasKeys('control'))) {
					$selection = [];
				}

				mouseDrag = [
					event.clientX,
					event.clientY + listElement.scrollTop,
					0,
					0,
					event.clientX,
					event.clientY + listElement.scrollTop,
					Array.from($selection)
				];
			} else if (type === 'move') {
				if (mouseDrag == null) {
					return;
				}

				updateBounds(event.clientX, event.clientY);
			}
		})();

		updateSelectedByBounds();
	}

	onMount(load);
</script>

<svelte:window on:blur={() => (mouseDrag = null)} />
{#if mouseDrag != null && mouseDragElement != null}
	<AnimationFrame
		callback={() => {
			if (!(mouseDrag != null && mouseDragElement != null)) {
				return;
			}

			const listArea = listElement.getBoundingClientRect();
			let addY = 0;

			const bottomDifference = listArea.bottom - mouseDrag[5] + listElement.scrollTop;
			if (bottomDifference < 64) {
				listElement.scrollBy(0, (addY = (64 - bottomDifference) / 2));
			}

			const topDifference = mouseDrag[5] - (listArea.top + listElement.scrollTop);
			if (topDifference < 64) {
				listElement.scrollBy(0, (addY = (-64 + topDifference) / 2));
			}

			if (
				listElement.scrollTop == 0 ||
				listElement.scrollTop + listArea.height >= listElement.scrollHeight
			) {
				addY = 0;
			}

			updateBounds(mouseDrag[4], mouseDrag[5] - listElement.scrollTop + addY);
			updateSelectedByBounds();
		}}
	/>
{/if}

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
	{#if info == null || isBusy}
		<LoadingSpinnerPage />
	{:else if files.length === 0}
		<div class="empty-banner">
			<i class="fa-regular fa-folder-open empty-icon"></i>
			<p>This folder seems empty.</p>
		</div>
	{:else}
		<div
			bind:this={listElement}
			class="file-list"
			role="none"
			on:mousemove={(event) => listenBackgroundEvent('move', event)}
			on:mouseup={(event) => listenBackgroundEvent('up', event)}
			on:mousedown={(event) => listenBackgroundEvent('down', event)}
			on:mouseleave={() => (mouseDrag = null)}
		>
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
		</div>
	{/if}
</div>

{#if mouseDrag != null}
	<div
		bind:this={mouseDragElement}
		class="mouse-drag"
		style={(([basisX, basisY, width, height]) => {
			if (width > 0 && height > 0) {
				return `left: ${basisX}px; top: ${basisY - listElement.scrollTop}px; width: ${width}px; height: ${height}px;`;
			} else if (width > 0 && height < 0) {
				return `left: ${basisX}px; top: ${basisY + height - listElement.scrollTop}px; width: ${width}px; height: ${-height}px;`;
			} else if (width < 0 && height > 0) {
				return `left: ${basisX + width}px; top: ${basisY - listElement.scrollTop}px; width: ${-width}px; height: ${height}px;`;
			} else if (width < 0 && height < 0) {
				return `left: ${basisX + width}px; top: ${basisY + height - listElement.scrollTop}px; width: ${-width}px; height: ${-height}px;`;
			}
		})(mouseDrag)}
	>
		<div></div>
	</div>
{/if}

<style lang="scss">
	div.mouse-drag {
		position: absolute;
		z-index: 2;
		pointer-events: none;

		box-sizing: border-box;
		border: solid 1px var(--primaryContainer);

		> div {
			width: 100%;
			height: 100%;

			background-color: var(--background);
			opacity: 0.5;
		}
	}

	div.file-list-container {
		display: flex;
		flex-direction: column;
		flex-grow: 1;
		align-items: center;
		background-color: var(--backgroundVariant);
		border-radius: 8px;

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
			flex-direction: row;
			flex-wrap: wrap;
			align-content: flex-start;
			gap: 16px;

			overflow-y: auto;
			overflow-x: hidden;
		}

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

	div.dnd {
		position: fixed;
		background-color: var(--backgroundVariant);
		color: var(--onBackgroundVariant);
		box-shadow: 2px 2px 8px var(--shadow);

		padding: 0.5em;
		border-radius: 8px;
		pointer-events: none;
	}
</style>
