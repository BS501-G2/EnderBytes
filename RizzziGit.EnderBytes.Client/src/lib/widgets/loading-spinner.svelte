<!-- https://www.benmvp.com/blog/how-to-create-circle-svg-gradient-loading-spinner -->

<script lang="ts" context="module">
	import { get, writable, type Writable } from 'svelte/store';

	let spinner: Writable<[degrees: number, time: number | null]> = writable([0, null]);
	let activeCount: number = 0;

	let degreesIncrement = 1000 / 360;

	function connect() {
		if (activeCount == 0) {
			const oldData = get(spinner);

			if (oldData[1] == null || oldData[1] < Date.now() - 10000) {
				spinner.set([0, null]);
			}

			const update = () => {
				if (activeCount <= 0) {
					activeCount = 0;

					return;
				}

				spinner.update((v) => {
					if (v[0] >= 360) {
						v[0] = 0;
					}

					return [v[0] + (Date.now() - (v[1] ?? Date.now())) / degreesIncrement, Date.now()];
				});

				requestAnimationFrame(update);
			};

			activeCount++;
			requestAnimationFrame(update);
			return;
		} else {
			activeCount++;
		}
	}

	function disconnect() {
		activeCount--;
	}

	export type Size = `${number}${'px' | 'em'}`;
</script>

<script lang="ts">
	import { onDestroy, onMount } from 'svelte';

	export let degrees: number = 0;
	export let size: Size | null = null;

	onMount(() => {
		connect();
	});

	onDestroy(() => disconnect());

	$: degrees = $spinner[0];
	$: sizeStyle = size != null
		? `min-width: ${size}; min-height: ${size}; max-width: ${size}; max-height: ${size};`
		: `min-width: 100%; min-height: 100%; max-width: 100%; max-height: 100%;`;
</script>

<svg
	viewBox="0 0 200 200"
	fill="none"
	style="transform: rotate({$spinner[0]}deg); {sizeStyle}"
	xmlns="http://www.w3.org/2000/svg"
>
	<defs>
		<linearGradient id="spinner-secondHalf">
			<stop offset="0%" stop-opacity="0" stop-color="currentColor" />
			<stop offset="100%" stop-opacity="0.5" stop-color="currentColor" />
		</linearGradient>
		<linearGradient id="spinner-firstHalf">
			<stop offset="0%" stop-opacity="1" stop-color="currentColor" />
			<stop offset="100%" stop-opacity="0.5" stop-color="currentColor" />
		</linearGradient>
	</defs>

	<g stroke-width="1">
		<path
			stroke="url(#spinner-secondHalf)"
			d="M 4 100 A 96 96 0 0 1 196 100"
			vector-effect="non-scaling-stroke"
		/>
		<path
			stroke="url(#spinner-firstHalf)"
			d="M 196 100 A 96 96 0 0 1 4 100"
			vector-effect="non-scaling-stroke"
		/>

		<path
			stroke="currentColor"
			stroke-linecap="round"
			d="M 4 100 A 96 96 0 0 1 4 98"
			vector-effect="non-scaling-stroke"
		/>
	</g>

	<!-- <animateTransform
	from="0 0 0"
	to="360 0 0"
	attributeName="transform"
	type="rotate"
	repeatCount="indefinite"
	dur="1300ms"
	/> -->
</svg>
