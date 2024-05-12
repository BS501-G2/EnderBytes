<script context="module" lang="ts">
	export enum InputClass {
		Primary = 'primary',
		PrimaryContainer = 'primary-container',
		Background = 'background'
	}
</script>

<script lang="ts">
	import { onMount } from 'svelte';

	export let inputClass: InputClass = InputClass.Background;
	export let type: HTMLInputElement['type'] = 'text';
	export let name: string;
	export let required: boolean = false;
	export let disabled: boolean = false;
	export let text: string = '';
	export let placeholder: string = 'Type something...';
	export let valid: boolean = true;
	export let autoFocus: boolean = false;
	export let validate: (() => boolean) | null = null;
	export let onSubmit: (() => Promise<void> | void) | null = null;

	let element: HTMLInputElement | null = null;
	let busy: boolean = false;

	async function submit() {
		if (busy) {
			return;
		}

		busy = true;
		try {
			await onSubmit?.();
		} finally {
			busy = false;
		}
	}

	const applyChange = () => {
		text = element?.value || '';
		valid = validate?.() ?? element?.checkValidity() ?? true;
	};

	onMount(() => {
		if (autoFocus && element != null) {
			element.focus();
		}
	});
</script>

<div class="input {inputClass}">
	<button on:click={() => element?.focus()}><p>{name}</p></button>
	<input
		bind:this={element}
		{type}
		{name}
		{required}
		disabled={disabled || busy}
		{placeholder}
		on:keydown={async (event) => {
			applyChange();

			if (event.key === 'Enter') {
				await submit();
			}
		}}
		on:change={applyChange}
	/>
</div>

<style lang="scss">
	div.input {
		color: var(--onPrimaryContainer);

		display: flex;
		flex-direction: column;

		cursor: text;

		padding: 4px 8px 4px 8px;

		border-radius: 8px;

		> button {
			background-color: unset;
			color: unset;
			border: unset;

			padding: 0px;

			font-size: 12px;

			cursor: unset;

			> p {
				margin: 0px;

				text-align: left;
			}
		}

		> input {
			background-color: var(--backgroundVariant);
			color: unset;
			border: unset;

			outline: none;

			font-size: 14px;
			border-radius: 8px;
		}
	}

	div.input.primary {
		border: solid 1px var(--onPrimary);

		> button {
			color: var(--onPrimary);
		}

		> input {
			background-color: var(--primary);
			color: var(--onPrimary);
		}
	}

	div.input.primary-container {
		border: solid 1px var(--primaryContainer);

		> button {
			color: var(--primaryContainer);
		}

		> input {
			background-color: var(--primaryContainer);
			color: var(--onPrimaryContainer);
		}
	}

	div.input.background {
		border: solid 1px var(--onBackground);

		> button {
			color: var(--onBackground);
		}

		> input {
			background-color: var(--backgroundVariant);
			color: var(--onBackgroundVariant);
		}
	}

	div.input:focus-within {
		outline: -webkit-focus-ring-color auto 1px;
	}
</style>
