<script lang="ts">
	import ProfilePage, { UserResolveType } from '../profile-page.svelte';
	import { session } from '$lib/client.svelte';

	$: userId = $session?.userId;
</script>

{#if userId != null}
	<ProfilePage
		identifier={{
			type: UserResolveType.UserId,
			userId
		}}
	/>
{/if}
