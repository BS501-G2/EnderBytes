<script lang="ts">
	import Button, { ButtonClass } from '$lib/widgets/button.svelte';
	import { scale } from 'svelte/transition';
	import type { FileBrowserState, FileResource } from '../../../file-browser.svelte';

	const {
		fileBrowserState,
		selection = $bindable()
	}: { fileBrowserState: FileBrowserState; selection: FileResource[] } = $props();

	type ControlBarItemGroup = 'new' | 'actions' | 'arrangement';

	interface ControlBarItem {
		label: string;
		icon: string;
		group: ControlBarItemGroup;
		action: () => Promise<void>;
	}

	function getControlBarItems(): ControlBarItem[] {
		const items: ControlBarItem[] = [];

		items.push({
			label: 'Sort',
			icon: 'fa-solid fa-sort',
			group: 'arrangement',
			action: async () => {}
		});

		if (!fileBrowserState.isLoading) {
			if (fileBrowserState.file?.isFolder) {
				if (fileBrowserState.allowCreate && (fileBrowserState.access?.highestExtent ?? 0 > 2)) {
					items.push({
						label: 'Upload',
						icon: 'fa-solid fa-file-circle-plus',
						group: 'new',
						action: async () => {}
					});

					items.push({
						label: 'New Folder',
						icon: 'fa-solid fa-folder-plus',
						group: 'new',
						action: async () => {}
					});
				}

				if (fileBrowserState.access?.highestExtent ?? 0 > 3) {
					if (selection.length > 0) {
						items.push({
							label: 'Trash',
							icon: 'fa-solid fa-trash',
							group: 'actions',
							action: async () => {}
						});
					}

					if (selection.length === 1) {
						items.push({
							label: 'Rename',
							icon: 'fa-solid fa-pencil',
							group: 'actions',
							action: async () => {}
						});

						items.push({
							label: 'Open',
							icon: 'fa-solid fa-folder-open',
							group: 'actions',
							action: async () => {}
						});
					}
				}
			}
		}

		return items;
	}

	const actions = $derived(getControlBarItems());
</script>

{#snippet buttons(actions: ControlBarItem[], animations: boolean)}
	{#each actions as action}
		{#snippet entry(action: ControlBarItem)}
			<i class="icon {action.icon}"></i>
			<p>{action.label}</p>
		{/snippet}

		<Button
			buttonClass={ButtonClass.BackgroundVariant}
			hint={action.label}
			outline={false}
			onClick={action.action}
		>
			{#if animations}
				<div class="button" transition:scale|global={{ duration: 200, start: 0.95 }}>
					{@render entry(action)}
				</div>
			{:else}
				<div class="button">{@render entry(action)}</div>
			{/if}
		</Button>
	{/each}
{/snippet}

<div class="control-bar-container">
	<div class="control-bar">
		{#snippet action(group: ControlBarItemGroup, animations: boolean)}
			{@const filteredActions = actions.filter((action) => action.group == group)}

			{#if filteredActions.length != 0}
				{#if animations}
					<div class="control-group" transition:scale|global={{ duration: 200, start: 0.95 }}>
						{@render buttons(filteredActions, animations)}
					</div>
				{:else}
					<div class="control-group">
						{@render buttons(filteredActions, animations)}
					</div>
				{/if}
			{/if}
		{/snippet}

		{@render action('new', true)}
		{@render action('actions', true)}
		<div class="spacer"></div>
		{@render action('arrangement', false)}
	</div>
</div>

<style lang="scss">
	div.control-bar-container {
		min-width: 0px;

		min-height: 2em;
		// max-height: 2em;

		overflow: auto hidden;

		user-select: none;

		box-sizing: border-box;

		display: flex;
		flex-direction: column;

		> div.control-bar {
			gap: 8px;

			display: flex;
			flex-direction: row;
			align-items: center;
			min-height: 2em;
			max-height: 2em;

			> div.spacer {
				flex-grow: 1;
			}

			> div.control-group {
				display: flex;
				flex-direction: row;
				align-items: center;

				background-color: var(--backgroundVariant);
				color: var(--onBackgroundVariant);
				border-radius: 8px;

				min-height: 2em;
				max-height: 2em;
				padding: 0px 2px;
			}
		}
	}

	div.button {
		display: flex;
		align-items: center;

		min-height: 2em;

		gap: 8px;

		> i.icon {
			min-height: 100%;
			max-height: 100%;

			font-size: 1.25em;
		}

		> p {
			text-wrap: nowrap;
		}
	}
</style>
