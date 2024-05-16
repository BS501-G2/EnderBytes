<script lang="ts" context="module">
	export enum FileBrowserListConfig {
		Table,
		Grid
	}

	const fileBrowserListStyle = 'fileBrowserListConfig';

	export function getFileBrowserListStyle(): FileBrowserListConfig {
		const config = JSON.parse(
			localStorage.getItem(fileBrowserListStyle) ?? 'null'
		) as FileBrowserListConfig | null;

		if (config != null && !(config in FileBrowserListConfig)) {
			return config;
		}

		return FileBrowserListConfig.Grid;
	}

	export function setFileBrowserListStyle(value: FileBrowserListConfig) {
		localStorage.setItem(fileBrowserListStyle, JSON.stringify(value));
	}
</script>

<script lang="ts">
	import { type FileBrowserState, type FileResource } from '../../../file-browser.svelte';
	import FileGridEntry from './file-list/file-grid-entry.svelte';
	import FileTableEntry from './file-list/file-table-entry.svelte';
	import { scale } from 'svelte/transition';

	import { hasKeys, AnimationFrame, LoadingSpinnerPage } from '@rizzzi/svelte-commons';

	let {
		fileBrowserState = $bindable(),
		selection = $bindable()
	}: { fileBrowserState: FileBrowserState; selection: FileResource[] } = $props();

	function click(fileBrowserState: FileBrowserState & { isLoading: false }, file: FileResource) {
		if (hasKeys('control')) {
			selection = !selection.includes(file)
				? [...selection, file]
				: selection.filter((id) => id !== file);
		} else if (hasKeys('shift')) {
			const { files } = fileBrowserState;

			if (selection.length === 0) {
				selection = [file];
			} else {
				const startIndex = files.indexOf(selection[0]);
				const endIndex = files.indexOf(file);

				if (startIndex > endIndex) {
					selection = files.slice(endIndex, startIndex + 1).toReversed();
				} else {
					selection = files.slice(startIndex, endIndex + 1);
				}
			}
		} else if (selection.length !== 1 || selection[0] !== file) {
			selection = [file];
		} else {
			selection = [];
		}
	}

	interface SelectionRectangle {
		cursorOriginX: number;
		cursorOriginY: number;
		cursorX: number;
		cursorY: number;
		clientX: number;
		clientY: number;

		capturedSelection: FileResource[];
	}

	let selectionBox: SelectionRectangle | null = $state(null);
	let selectionBoxElement: HTMLDivElement | null = $state(null);

	let fileListContianer: HTMLDivElement | null = $state(null);
	let fileList: HTMLDivElement | null = $state(null);
	let fileListInner: HTMLDivElement | null = $state(null);

	function setSelectionRectangle(cursorX: number, cursorY: number) {
		const scrollTop = fileList?.scrollTop ?? 0;
		const { left, top } = fileListContianer!.getBoundingClientRect();

		selectionBox = {
			cursorOriginX: selectionBox?.cursorOriginX ?? cursorX - left,
			cursorOriginY: selectionBox?.cursorOriginY ?? cursorY - top + scrollTop,
			cursorX: cursorX - left,
			cursorY: cursorY - top + scrollTop,

			clientX: cursorX,
			clientY: cursorY,

			capturedSelection: hasKeys('shift') || hasKeys('control')
				? selectionBox?.capturedSelection ?? [...selection]
				: []
		};
	}

	function clearSelectionRectangle() {
		selectionBox = null;
	}
</script>

{#if selectionBox != null}
	<AnimationFrame
		callback={() => {
			if (selectionBox == null || fileBrowserState.isLoading) {
				return;
			}

			const { clientY } = selectionBox!;
			const rect = fileListContianer?.getBoundingClientRect();

			if (rect == null) {
				return;
			}

			const topY = 64 - Math.max( Math.min(-(rect.top - clientY), 64), 0);
			const bottomY	= 64 - Math.max(Math.min(rect.bottom - clientY, 64), 0);

			if (topY > 0) {
				fileList!.scrollTop -= topY / 4;
			} else if (bottomY > 0) {
				fileList!.scrollTop += bottomY / 4;
			}

			setSelectionRectangle(selectionBox.clientX, selectionBox.clientY);

			const files = fileBrowserState.files;
			let selectedFiles = [...selectionBox.capturedSelection];

			for (let index = 0; index < files.length; index++) {
				const file = files[index];
				const fileElement = fileListInner!.children[index] as HTMLDivElement;
				const fileRect = fileElement.getBoundingClientRect();
				const fileSelectionRect = selectionBoxElement?.getBoundingClientRect()

				if (fileSelectionRect == null) {
					continue;
				}

				if (
					fileRect.x < fileSelectionRect.x + fileSelectionRect.width &&
					fileRect.x + fileRect.width > fileSelectionRect.x &&
					fileRect.y < fileSelectionRect.y + fileSelectionRect.height &&
					fileRect.y + fileRect.height > fileSelectionRect.y
				) {
					if (hasKeys('shift') && !selectedFiles.includes(file)) {
						selectedFiles.push(file)
					} else if (hasKeys('control') && selectedFiles.includes(file)) {
						const fileIndex = selectedFiles.indexOf(file);
						if (fileIndex >= 0) {
							selectedFiles.splice(fileIndex, 1);
						}
					} else if (!selectedFiles.includes(file)) {
						selectedFiles.push(file)
					}
				}
			}

			selection = selectedFiles;
		}}
	/>
{/if}

<div class="file-list-container" bind:this={fileListContianer}>
	{#if fileBrowserState.isLoading}
		<LoadingSpinnerPage />
	{:else}
		<div
			role="none"
			class="file-list-inner-container"
			transition:scale|global={{ start: 0.95, duration: 200 }}
			bind:this={fileList}
			onmousedown={(e) => {
				if (fileList != e.target && fileListInner != e.target) {
					return;
				}

				if (e.currentTarget.clientWidth < (e.clientX - e.currentTarget.offsetLeft)) {
					return;
				}

				setSelectionRectangle(e.clientX, e.clientY);
			}}
			onmousemove={(e) => {
				if (selectionBox == null) {
					return;
				}

				setSelectionRectangle(e.clientX, e.clientY);
			}}
			onmouseup={clearSelectionRectangle}
		>
			{#if selectionBox != null}
				{@const { cursorOriginX, cursorOriginY, cursorX, cursorY } = selectionBox}
				{@const x = cursorOriginX}
				{@const y = cursorOriginY}
				{@const width = cursorX - cursorOriginX}
				{@const height = cursorY - cursorOriginY}

				{#snippet triangle(x: number, y: number, w: number, h: number)}
					<div class="selection-rectangle-container">
						<div
							class="selection-rectangle-inner-container"
							style="padding-left: {x}px; padding-top: {y}px;"
						>
							<div
								bind:this={selectionBoxElement}
								class="selection-rectangle"
								style="width: {w}px; height: {h}px;"
							>
								<div class="selection-rectangle-inner"></div>
							</div>
						</div>
					</div>
				{/snippet}

				{#if width > 0 && height > 0}
					{@render triangle(x, y, width, height)}
				{:else if width > 0 && height < 0}
					{@render triangle(x, y + height, width, -height)}
				{:else if width < 0 && height > 0}
					{@render triangle(x + width, y, -width, height)}
				{:else if width < 0 && height < 0}
					{@render triangle(x + width, y + height, -width, -height)}
				{/if}
			{/if}
			{#if getFileBrowserListStyle() === FileBrowserListConfig.Grid}
				<div class="file-grid" bind:this={fileListInner}>
					{#each fileBrowserState.files as file}
						<FileGridEntry {fileBrowserState} {file} bind:selection onClick={click} />
					{/each}
				</div>
			{:else}
				<div class="file-table" bind:this={fileListInner}>
					{#each fileBrowserState.files as file}
						<FileTableEntry {fileBrowserState} {file} bind:selection onClick={click} />
					{/each}
				</div>
			{/if}
		</div>
	{/if}
</div>

<style lang="scss">
	div.file-list-container {
		flex-grow: 1;
		min-height: 0px;

		border-radius: 8px;

		background-color: var(--backgroundVariant);
		color: var(--onBackgroundVariant);

		display: flex;
		flex-direction: column;

		user-select: none;

		> div.file-list-inner-container {
			flex-grow: 1;
			min-height: 0px;

			overflow: hidden auto;

			> div.file-grid {
				display: grid;

				margin: 16px;
				gap: 16px;
				grid-template-columns: repeat(auto-fill, minmax(172px, 1fr));
			}

			> div.file-table {
				display: flex;
				flex-direction: column;
				gap: 8px;
				margin: 8px;
				overflow: auto;
			}
		}
	}

	div.selection-rectangle-container {
		max-width: 0px;
		max-height: 0px;

		pointer-events: none;

		> div.selection-rectangle-inner-container {
			> div.selection-rectangle {
				border: 1px solid var(--onBackground);
				background-color: var(--background);

				opacity: 0.5;
			}
		}
	}
</style>
