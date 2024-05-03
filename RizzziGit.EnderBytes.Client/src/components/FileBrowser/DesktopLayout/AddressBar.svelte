<script lang="ts">
	import { FolderIcon } from 'svelte-feather-icons';

	import AddressBarEntry from './AddressBar/AddressBarEntry.svelte';
	import LoadingSpinner from '../../Widgets/LoadingSpinner.svelte';
	import type { FileBrowserInformation } from '../../FileBrowser.svelte';

	export let info: FileBrowserInformation | null;
</script>

<div class="address">
	<div class="address-content">
		<div class="icon">
			<FolderIcon size="20em" />
		</div>
		{#if info == null}
			<LoadingSpinner size="1em" />
		{:else}
			{@const { isSharePoint, root, chain } = info.pathChain}
			{@const files = [isSharePoint ? root : null, ...chain]}

			<div class="path-chain">
				{#each files as _, index}
					<AddressBarEntry {files} {index} />
				{/each}
			</div>
		{/if}
	</div>
</div>

<style lang="scss">
	div.address {
		background-color: var(--backgroundVariant);
		color: var(--onBackgroundVariant);

		display: flex;
		align-items: center;

		border-radius: 0.5em;
		padding: 0.25em 0.5em 0.25em 0.5em;

		> div.address-content {
			> div.icon,
			div.path-chain {
				display: flex;

				align-items: center;
			}

			flex-grow: 1;

			display: flex;

			gap: 8px;
			align-items: center;

			min-height: calc(1em  + 14px);
			max-height: calc(1em  + 14px);

			overflow-x: auto;
			overflow-y: hidden;
		}
	}
</style>
