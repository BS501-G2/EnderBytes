<script lang="ts" context="module">
	export type AwaiterSetStatusFunction = (
		message?: string | null,
		progressPercentage?: number | null
	) => void;
	export type AwaiterResetFunction = (autoLoad?: boolean) => Promise<void>;
	export type AwaiterLoadFunction = () => Promise<void>;
	export type AwaiterRetryFunction = () => void;
	export type AwaiterCallback<T> = (setStatus: AwaiterSetStatusFunction) => T | Promise<T>;
</script>

<script lang="ts" generics="T extends any">
	import type { Size } from './widgets/loading-spinner.svelte';

	import { onMount } from 'svelte';

	import LoadingPage from './widgets/loading-spinner-page.svelte';
	import Banner, { BannerClass } from './widgets/banner.svelte';
	import Button, { ButtonClass } from './widgets/button.svelte';

	export let callback: AwaiterCallback<T>;
	export let autoLoad = true;
	export let loadingSize: Size | null = null;

	let promise: Promise<T>;
	let message: string | null = null;
	let progress: number | null = null;

	const setStatus: AwaiterSetStatusFunction = (newMessage = message, newProgress = progress) => {
		message = newMessage;
		progress = newProgress;
	};

	let busy: boolean = false;

	async function exec() {
		if (busy) {
			return;
		}

		busy = true;
		try {
			try {
				const result = callback(setStatus);

				if (result != null) {
					promise = (async () => await result)();
				}
			} catch (error: any) {
				promise = Promise.reject(error);
			}

			await promise;
		} finally {
			busy = false;
		}
	}

	onMount(() => {
		if (autoLoad) {
			exec();
		}
	});

	export const load: () => Promise<void> = async () => {
		if (promise != null) {
			return;
		}

		await exec();
	};

	export const reset: (autoLoad?: boolean) => Promise<void> = async (load = autoLoad) => {
		if (busy) {
			return;
		}

		promise = <any>undefined;

		if (load) {
			await exec();
		}
	};

	interface $$Slots {
		'not-loaded': {
			load: AwaiterLoadFunction;
		};
		loading: {
			message: string | null;
		};
		'loading-page': {
			message: string | null;
		};
		'loading-page-without-spinner': {
			message: string | null;
		};
		success: {
			result: T;
		};
		error: {
			error: any;
			reset: AwaiterResetFunction;
			retry: AwaiterRetryFunction;
		};
	}
</script>

{#key promise}
	{#if promise == null}
		{#if $$slots['not-loaded']}
			<slot name="not-loaded" load={exec} />
		{:else if $$slots.loading}
			<slot name="loading" {message} />
		{:else}
			<Banner bannerClass={BannerClass.Info}>
				<div class="banner">
					<p style="margin: 0">Not loaded.</p>
					<Button onClick={exec} buttonClass={ButtonClass.Background}>Load</Button>
				</div>
			</Banner>
		{/if}
	{:else}
		{#await promise}
			{#if $$slots.loading}
				<slot name="loading" {message} />
			{:else if $$slots['loading-page-without-spinner']}
				<LoadingPage {loadingSize}>
					<svelte:fragment slot="without-spinner">
						<slot name="loading-page-without-spinner" {message} />
					</svelte:fragment>
				</LoadingPage>
			{:else if $$slots['loading-page']}
				<LoadingPage {loadingSize}>
					<svelte:fragment>
						<slot name="loading-page" {message} />
					</svelte:fragment>
				</LoadingPage>
			{:else}
				<LoadingPage {loadingSize}>
					<svelte:fragment>
						{#if message == null || progress == null}
							<p>
								{#if message != null}
									{message}
								{/if}

								{#if progress != null}
									{Math.floor(progress * 100)}%
								{/if}
							</p>
						{/if}
					</svelte:fragment>
				</LoadingPage>
			{/if}
		{:then result}
			<slot name="success" {result} />
		{:catch error}
			{#if $$slots.error}
				<slot name="error" {error} retry={exec} {reset} />
			{:else}
				<div class="content">
					<Banner bannerClass={BannerClass.Error}>
						<div class="banner">
							<p style="margin: 0">Error: {error.message}</p>
							<Button onClick={() => reset()} buttonClass={ButtonClass.Background}>Retry</Button>
						</div>
					</Banner>
				</div>
			{/if}
		{/await}
	{/if}
{/key}

<style lang="scss">
	div.content {
		display: flex;
		flex-direction: column;
		gap: 8px;
		justify-content: center;

		width: 100%;
		height: 100%;

		div.banner {
			display: flex;
			flex-direction: column;
			gap: 8px;
		}
	}
</style>
