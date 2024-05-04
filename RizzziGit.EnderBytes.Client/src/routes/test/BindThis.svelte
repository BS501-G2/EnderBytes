<script lang="ts">
	import { onMount } from 'svelte';

	let a: HTMLElement | null = null;
	let b: boolean = false;

	onMount(() => {
		setInterval(() => {
			b = !b;
		}, 100);
	});

	$: console.log(a);
</script>

{#if b}
	<p bind:this={a}></p>
{/if}
