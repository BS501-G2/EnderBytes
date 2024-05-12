<script lang="ts">
	import type { FileResource } from '../../../../file-browser.svelte';

	const {
		file,
		forward = false,
		onMenu,
		onClick
	}: {
		file: FileResource;
		forward?: boolean;
		onMenu: (event: MouseEvent) => void;
		onClick: (event: MouseEvent) => void;
	} = $props();
</script>

<div class="path-chain-entry">
	{#snippet link()}
		<button class="link {forward ? 'forward' : ''}" onclick={onClick}>
			<p>{file.name}</p>
		</button>
	{/snippet}

	{#snippet expand()}
		<button class="menu-button {forward ? 'forward' : ''}" onclick={onMenu}>
			<i class="fa-solid fa-chevron-right"></i>
		</button>
	{/snippet}

	{#if forward}
		{@render link()}
		{@render expand()}
	{:else}
		{@render expand()}
		{@render link()}
	{/if}
</div>

<style lang="scss">
	div.path-chain-entry {
		display: flex;
		flex-direction: row;

		user-select: none;

		color: var(--onBackgroundVariant);

		max-width: 256px;

		border-radius: 8px;
		transition: all linear 150ms;
		border: 1px solid transparent;

		> button {
			min-width: 0px;
			background-color: unset;
			border: unset;

			padding: 4px 8px;

			color: inherit;
			transition: all linear 150ms;

			text-overflow: ellipsis;
			text-wrap: nowrap;

			display: flex;
			flex-direction: column;
			align-items: left;
			justify-content: center;

			cursor: pointer;
		}

		> button:hover {
			background-color: var(--background);
			color: var(--onBackground);
		}

		> button.menu-button {
			border-radius: 8px 0px 0px 8px;
		}

		> button.link {
			border-radius: 0px 8px 8px 0px;
			flex-grow: 1;

			> p {
				min-height: 1em;
				overflow: hidden;

				text-align: left;
				text-wrap: nowrap;
				text-overflow: ellipsis;
			}
		}

		> button.menu-button.forward {
			border-radius: 0px 8px 8px 0px;
		}

		> button.link.forward {
			border-radius: 8px 0px 0px 8px;
		}
	}

	div.path-chain-entry:hover {
		border-color: var(--background);
	}
</style>
