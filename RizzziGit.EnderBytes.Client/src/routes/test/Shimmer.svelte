<script lang="ts">
	import type { Snippet } from 'svelte';
	import AnimationFrame from '../../components/Bindings/AnimationFrame.svelte';

	let {
		shimmer,
		children,
		shimmering
	}: { shimmer: boolean; children: Snippet; shimmering?: Snippet } = $props();
</script>

{#if !shimmer}
	{@render children()}
{:else}
	<AnimationFrame
		callback={(_0, _1, value: number = 0) => Math.min((value + 0.01) % 1, 1)}
		let:output
	>
		<!-- <p>{output}</p> -->
		<div class="shimmer">
			{#if shimmering}
				{@render shimmering()}
			{:else}
				{@render children()}
			{/if}
		</div>
	</AnimationFrame>
{/if}

<style lang="scss">
  div.shimmer {

  }
</style>
