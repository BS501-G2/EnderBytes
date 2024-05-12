<script lang="ts" context="module">
	export enum DialogClass {
		Normal = 'normal',
		Warning = 'warning',
		Error = 'error'
	}
</script>

<script lang="ts">
	import { XIcon } from 'svelte-feather-icons';

	import Button, { ButtonClass } from './button.svelte';
	import { scale } from 'svelte/transition';
	import ResponsiveLayout from '$lib/responsive-layout.svelte';
	import Overlay from './overlay.svelte';
	import { ViewMode, viewMode } from '$lib/responsive-layout.svelte';
	import type { Snippet } from 'svelte';

	const {
		onDismiss,
		dialogClass = DialogClass.Normal,
		head,
		body,
		actions
	}: {
		onDismiss: () => void;
		dialogClass?: DialogClass;
		head?: Snippet;
		body?: Snippet;
		actions?: Snippet;
	} = $props();
</script>

<Overlay {onDismiss} dim={true}>
	<div
		class="dialog {dialogClass} {$viewMode & ViewMode.Mobile ? 'mobile' : ''}"
		transition:scale|global={{ duration: 200, start: 0.9 }}
	>
		{#if head}
			<ResponsiveLayout>
				{#snippet desktop()}
					<div class="head">
						{@render head()}
					</div>
				{/snippet}
				{#snippet mobile()}
					<div class="head-mobile">
						<div class="head-element">
							{@render head()}
						</div>
						<Button buttonClass={ButtonClass.Background} onClick={() => onDismiss()}>
							<XIcon />
						</Button>
					</div>
				{/snippet}
			</ResponsiveLayout>
		{/if}
		{#if body}
			<div
				class="body"
				style="{head ? 'margin-top: 16px;' : ''} {actions ? 'margin-bottom: 16px' : ''}"
			>
				{@render body()}
			</div>
		{/if}
		{#if actions}
			<div class="actions">
				{@render actions()}
			</div>
		{/if}
	</div>
</Overlay>

<style lang="scss">
	div.dialog {
		max-width: calc(100vw - 32px);
		max-height: calc(100vh - 32px);
		min-height: 128px;

		box-sizing: border-box;

		padding: 16px;
		// border: solid 1px var(--primary);
		border-radius: 8px;
		box-shadow: gray 2px 2px 8px;

		display: flex;
		flex-direction: column;
		justify-content: space-between;

		gap: 8px;

		div.head-mobile {
			display: flex;

			> div.head-element {
				flex-grow: 1;
			}
		}

		> div.head {
			text-align: left;
		}

		> div.body {
			flex-grow: 1;
			text-align: left;

			box-sizing: border-box;

			overflow-y: auto;
			overflow-x: hidden;
		}

		> div.actions {
			display: flex;

			gap: 8px;
			justify-content: flex-end;
		}
	}

	div.dialog.mobile {
		width: 100vw;
		height: 100vh;

		max-width: 100vw;
		max-height: 100vh;

		padding: 16px;

		border: none;
		border-radius: 0px;
	}

	div.dialog.normal {
		background-color: var(--backgroundVariant);
		color: var(--onBackgroundVariant);
	}

	div.dialog.warning {
		background-color: #ffffa0;
		color: var(--onBackground);
	}

	div.dialog.error {
		background-color: #ffa0a0;
		color: var(--onBackground);
	}
</style>
