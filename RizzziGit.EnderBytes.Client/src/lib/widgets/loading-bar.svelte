<script lang="ts">
	import AnimationFrame from '$lib/animation-frame.svelte';

	export let progress: number | null = null;

	let payload = { offset: 0 };

	let barWidth: number = 0;
	let thumbWidth: number = 0;

	$: maxWidth = barWidth + thumbWidth;
	$: increment = 1000 / (maxWidth / 2);
</script>

{#if progress == null}
	<AnimationFrame
		callback={(previous, time) => {
			previous ??= time;

			if (payload.offset >= maxWidth) {
				payload.offset = 0;
			}

			payload.offset += (time - previous) / increment;
		}}
	>
		<div class="loading-bar" bind:clientWidth={barWidth}>
			<div
				class="loading-thumb noprogress"
				bind:clientWidth={thumbWidth}
				style="margin-left: {payload.offset - thumbWidth}px;"
			></div>
		</div>
	</AnimationFrame>
{:else}
	<div class="loading-bar" bind:clientWidth={barWidth}>
		<div
			class="loading-thumb"
			bind:clientWidth={thumbWidth}
			style="min-width: {barWidth * Math.max(Math.min(progress, 1), 0)}px;"
		></div>
	</div>
{/if}

<style lang="scss">
	div.loading-bar {
		width: 100%;
		height: 4px;

		background-color: var(--primaryContainer);

		display: flex;

		overflow-x: hidden;

		> div.loading-thumb {
			background-color: var(--primary);
		}

		> div.loading-thumb.noprogress {
			min-width: 25%;
		}
	}
</style>
