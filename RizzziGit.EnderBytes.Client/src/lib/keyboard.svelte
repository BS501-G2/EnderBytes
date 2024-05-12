<script lang="ts" context="module">
	export interface KeyboardListener {
		keys: string[];

		func: () => Promise<void> | void;
	}

	let keys: string[] = [];

	export function hasKeys(...findKeys: string[]): boolean {
		return findKeys.every((key) => keys.includes(key));
	}
</script>

<script lang="ts">
	function onKey(type: 'up' | 'down', event: KeyboardEvent) {
		const key = event.key?.toLowerCase() ?? '';

		if (type == 'down' && !keys.includes(key)) {
			keys.push(key);
		} else if (type == 'up' && keys.includes(key)) {
			const index = keys.indexOf(key);

			keys.splice(index, 1);
		}
	}
</script>

<svelte:window
	on:keydown={(event) => onKey('down', event)}
	on:keyup={(event) => onKey('up', event)}
	on:blur={() => keys.splice(0, keys.length)}
/>
