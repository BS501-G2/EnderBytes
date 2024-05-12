<script lang="ts" context="module">
	export enum ButtonClass {
		Primary = 'primary',
		PrimaryContainer = 'primary-container',
		Background = 'background',
		BackgroundVariant = 'background-variant'
	}
</script>

<script lang="ts">
	import Awaiter from '$lib/awaiter.svelte';
	import LoadingSpinner from './loading-spinner.svelte';

	export let buttonClass: ButtonClass = ButtonClass.Primary;
	export let enabled: boolean = true;
	export let outline: boolean = true;
	export let onClick: () => Promise<any> | any;
	export let hint: string | null = null;

	let busy: boolean = false;

	async function click() {
		try {
			busy = true;

			return await onClick();
		} finally {
			reset();
			busy = false;
		}
	}

	let reset: (autoLoad?: boolean | undefined) => Promise<void>;
</script>

<button
	disabled={!enabled || busy}
	class="button {buttonClass} {outline ? 'outline' : ''}"
	on:click={onClick}
	title={hint}
>
	<Awaiter callback={() => click()} autoLoad={false} bind:reset>
		<svelte:fragment slot="loading">
			<LoadingSpinner size="18px"></LoadingSpinner>
		</svelte:fragment>
		<svelte:fragment slot="not-loaded">
			<slot />
		</svelte:fragment>
	</Awaiter>
</button>

<style lang="scss">
	button.button {
		// padding: 8px 16px 8px 16px;

		box-sizing: border-box;
		transition: all linear 150ms;

		border: solid 1px transparent;
		border-radius: 8px;

		cursor: pointer;
	}

	button.button.outline {
		border-color: var(--primary);
	}

	button.primary {
		background-color: var(--primary);
		color: var(--onPrimary);
	}

	button.primary:hover {
		background-color: var(--primaryVariant);
		color: var(--onPrimaryVariant);
	}

	button.primary:active {
		background-color: var(--onPrimary);
	}

	button.primary-container {
		background-color: var(--primaryContainer);
		color: var(--onPrimaryContainer);
	}

	button.primary-container:hover {
		background-color: var(--primaryContainerVariant);
		color: var(--onPrimaryContainer);
	}

	button.primary-container:active {
		background-color: var(--onPrimaryContainer);
		color: var(--onPrimary);
	}

	button.background {
		background-color: var(--background);
		color: var(--onBackground);
	}

	button.background:hover {
		background-color: var(--backgroundVariant);
		color: var(--onBackground);
	}

	button.background:active {
		background-color: var(--onBackground);
		color: var(--background);
	}

	button.background-variant {
		background-color: var(--backgroundVariant);
		color: var(--onBackground);
	}

	button.background-variant:hover {
		background-color: var(--background);
		color: var(--onBackground);
	}

	button.background-variant:active {
		background-color: var(--onBackground);
		color: var(--backgroundVariant);
	}

	button:disabled {
		cursor: not-allowed;
	}

	button:disabled.primary {
		background-color: var(--primary);
		color: var(--onPrimary);
	}

	button:disabled.primary-container {
		background-color: var(--primaryContainer);
		color: var(--onPrimaryContainer);
	}

	button:disabled.background {
		background-color: var(--background);
		color: var(--onBackground);
	}
</style>
