<script lang="ts">
	import { slide } from 'svelte/transition';

	export let expanded: boolean = false;
</script>

<div class="expandable-header">
	<slot
		name="header"
		{expanded}
		toggle={() => {
			expanded = !expanded;
		}}
	/>
</div>
{#if expanded}
	<div class="expandable-body" transition:slide|global={{ duration: 200 }}>
		<slot name="body" />
	</div>
{/if}
