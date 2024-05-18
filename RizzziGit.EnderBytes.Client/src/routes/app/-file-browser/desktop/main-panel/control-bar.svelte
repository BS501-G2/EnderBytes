<script lang="ts" context="module">
	export type ControlBarItemGroup = 'new' | 'actions' | 'arrangement';

	export interface ControlBarItem {
		label: string;
		icon: string;
		group: ControlBarItemGroup;
		action: (event: MouseEvent) => Promise<void>;
	}
</script>

<script lang="ts">
	import { Button, ButtonClass } from '@rizzzi/svelte-commons';
	import { scale } from 'svelte/transition';
	import type { FileBrowserState, FileResource } from '../../../file-browser.svelte';

	let {
		fileBrowserState = $bindable(),
		selection = $bindable()
	}: { fileBrowserState: FileBrowserState; selection: FileResource[] } = $props();
</script>

{#snippet buttons(actions: ControlBarItem[], animations: boolean)}
	{#each actions as action}
		{#snippet entry(action: ControlBarItem)}
			<i class="icon {action.icon}"></i>
			<p>{action.label}</p>
		{/snippet}

		<div class="button-entry" transition:scale|global={{ duration: 200, start: 0.95 }}>
			<Button
				buttonClass={ButtonClass.BackgroundVariant}
				hint={action.label}
				outline={false}
				onClick={action.action}
			>
				{#if animations}
					<div class="button">
						{@render entry(action)}
					</div>
				{:else}
					<div class="button">{@render entry(action)}</div>
				{/if}
			</Button>
		</div>
	{/each}
{/snippet}

{#if fileBrowserState.controlBarActions != null}
	{@const actions = fileBrowserState.controlBarActions}

	<div class="control-bar-container">
		<div class="control-bar">
			{#snippet action(actions: ControlBarItem[], group: ControlBarItemGroup, animations: boolean)}
				{@const filteredActions = actions?.filter((action) => action.group == group) ?? []}

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

			{@render action(actions, 'new', true)}
			{@render action(actions, 'actions', true)}
			<div class="spacer"></div>
			{@render action(actions, 'arrangement', false)}
		</div>
	</div>
{/if}

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

	div.button-entry {
		display: contents;
	}

  div.button {
		display: flex;
		align-items: center;

		min-height: 2em;

		gap: 8px;
	}
	i.icon {
		min-height: 100%;
		max-height: 100%;

		font-size: 1.25em;
	}

	p {
		text-wrap: nowrap;
	}
</style>
