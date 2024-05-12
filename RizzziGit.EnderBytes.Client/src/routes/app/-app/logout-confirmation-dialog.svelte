<script lang="ts" context="module">
	import { writable, type Writable } from 'svelte/store';

	export const enabled: Writable<boolean> = writable(false);
</script>

<script lang="ts">
	import Client from '$lib/client.svelte';
	import Button, { ButtonClass } from '$lib/widgets/button.svelte';
	import Dialog, { DialogClass } from '$lib/widgets/dialog.svelte';
</script>

{#if $enabled}
	<Dialog dialogClass={DialogClass.Normal} onDismiss={() => ($enabled = false)}>
		{#snippet actions()}
			<Client let:apiFetch>
				<Button
					onClick={() => {
						$enabled = false;
						return apiFetch({ path: '/auth/logout', method: 'POST' });
					}}
					buttonClass={ButtonClass.Primary}
				>
					<p class="label">OK</p>
				</Button>
				<Button onClick={() => ($enabled = false)} buttonClass={ButtonClass.Background}>
					<p class="label">Cancel</p>
				</Button>
			</Client>
		{/snippet}
		{#snippet head()}
			<h2 style="margin: 0px;">Account Logout</h2>
		{/snippet}
		{#snippet body()}
			<span>This will log you out from the dashboard.</span>
		{/snippet}
	</Dialog>
{/if}

<style lang="scss">
	p.label {
		margin: 8px;
	}
</style>
