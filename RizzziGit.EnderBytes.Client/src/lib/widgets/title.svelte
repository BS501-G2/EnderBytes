<script lang="ts" context="module">
	export const titleStack: Writable<{ title: string }[]> = writable([]);
	export const titleString: Readable<string> = derived(titleStack, (titleStack) => {
		return titleStack
			.toReversed()
			.map(({ title }) => title)
			.join(' - ');
	});
</script>

<script lang="ts">
	import { onDestroy, onMount, type Snippet } from 'svelte';
	import type { Readable } from 'svelte/motion';
	import { derived, writable, type Writable } from 'svelte/store';

	const {
		title,
		children,
		last = false
	}: { title: string; children?: Snippet<[string]>; last?: boolean } = $props();

	const entry = { title };

	onMount(() => {
		if (last) {
			$titleStack.unshift(entry);
		} else {
			$titleStack.push(entry);
		}
		$titleStack = $titleStack;
	});

	onDestroy(() => {
		const index = $titleStack.indexOf(entry);
		if (index >= 0) {
			$titleStack.splice(index, 1);
			$titleStack = $titleStack;
		}
	});
</script>

{#if children != null}
	{@render children(title)}
{/if}
