<script lang="ts">
	import { fade, scale } from 'svelte/transition';
	import { tweened } from 'svelte/motion';
	import { type Writable, writable } from 'svelte/store';
	import { cubicOut, bounceOut, quintOut, quintInOut } from 'svelte/easing';
	import Test from './Test.svelte';

	const xT = tweened(0, { easing: cubicOut, duration: 500 });
	const yT = tweened(0, { easing: cubicOut, duration: 500 });

	const trails: Writable<[x: number, y: number, message: string | null][]> = writable([]);

	let race: [x: number | null, y: number | null] = [null, null];
	let lastRace = $trails[$trails.length - 1] ?? [0, 0];

	function update(x: number | null = race[0], y: number | null = race[1]) {
		race = [x, y];

		if (x != null && y != null) {
			if (lastRace[0] - x > 50 || Math.abs(lastRace[1] - y) > 50) {
				$trails.push([x, y, `x: ${Math.round(x)} y: ${Math.round(y)}`]);

				lastRace = <any>[x, y];
			} else {
				$trails.push([x, y, null]);
			}
			$trails.splice(0, Math.max($trails.length - 100, 0));
			$trails = $trails;

			race = [null, null];
		}
	}

	xT.subscribe((value) => {
		update(value, undefined);
	});

	yT.subscribe((value) => {
		update(undefined, value);
	});

	let a = $state(false);
</script>

<svelte:window
	on:mousemove={(event) => {
		const x = event.clientX - 50;
		const y = event.clientY - 50;

		$xT = x;
		$yT = y;
	}}
/>

{#each $trails as [x, y, message]}
	<div class="a trail" style="left: {x}px; top: {y}px;">
		{message ?? ''}
	</div>
{/each}

<div class="a" style="left: {$xT}px; top: {$yT}px;"></div>

<button onclick={() => (a = !a)}>asdas</button>

{@debug a}
{#if a}
	<Test></Test>
{/if}

<style lang="scss">
	div.a {
		position: fixed;

		background-color: rgba(0, 0, 0, 0.01);

		width: 100px;
		height: 100px;

		border-radius: 50%;

		z-index: 0;
		pointer-events: none;
	}

	div.a.trail {
		background-color: rgba(0, 0, 0, 0.01);

		text-wrap: nowrap;

		// box-shadow: 0px 0px 50px rgba(0, 0, 0, 0.25);
	}
</style>
