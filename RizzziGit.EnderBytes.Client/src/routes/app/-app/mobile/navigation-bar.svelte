<script lang="ts">
	import { HomeIcon } from 'svelte-feather-icons';

	interface NavigationEntry {
		name: string;
		icon: any;
		path: string;
	}
</script>

<div class="navigation-bar">
	<button><HomeIcon /></button>
</div>

<style lang="scss">
	div.navigation-bar {
		display: flex;
		flex-direction: row;
		justify-content: space-between;

		gap: 3em;

		> button {
			flex-grow: 1;
		}
	}
</style>
