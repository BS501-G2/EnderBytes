<script lang="ts" context="module">
	import { onDestroy, onMount } from 'svelte';
	import { get, writable, type Writable } from 'svelte/store';

	export type FrameCallback<T> = (previousTime: number | null, time: number, value?: T) => T;

	export interface FrameListener<T> {
		previousTime: number | null;

		callback: FrameCallback<T>;
	}

	let listeners: Writable<FrameListener<any>[]> = writable([]);
	let running: Writable<boolean> = writable(false);

	listeners.subscribe((updatedValue) => {
		const update = () => {
			if (!get(running)) {
				return;
			}

			const cachedListeners = get(listeners);

			if (!cachedListeners.length) {
				running.set(false);
			}

			for (const listener of cachedListeners) {
				try {
					listener.callback(listener.previousTime, (listener.previousTime = Date.now()));
				} catch (error: any) {
					console.error(error);
				}
			}

			requestAnimationFrame(update);
		};

		if (updatedValue.length !== 0 && !get(running)) {
			running.set(true);
			requestAnimationFrame(update);
		}
	});
</script>

<script lang="ts" generics="T extends any">
	export let callback: FrameCallback<T>;

	let output: [T] | null = null;

	const frameListener: FrameListener<T> = {
		previousTime: null,
		callback: (previousTime, time) => (output = [callback(previousTime, time, output?.[0])])[0]
	};

	onMount(() => {
		listeners.update((value) => {
			value.push(frameListener);

			return value;
		});
	});

	onDestroy(() => {
		listeners.update((value) => {
			const index = value.indexOf(frameListener);

			if (index >= 0) {
				value.splice(index, 1);
			}

			return value;
		});
	});

	interface $$Slots {
		default: { output: T };
	}
</script>

{#if output != null}
	<slot output={output[0]} />
{/if}
