<script lang="ts">
	import ProfilePage, { UserResolveType } from '../../../../components/ProfilePage.svelte';
	import { session } from '../../../../components/Bindings/Client.svelte';

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
