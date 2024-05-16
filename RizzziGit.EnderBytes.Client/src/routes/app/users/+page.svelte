<script lang="ts">
	import { page } from '$app/stores';

	import ProfilePage, { type UserResolve, UserResolveType } from './profile-page.svelte';
	import { session } from '$lib/client.svelte';

	const parse = (): UserResolve | null => {
		const idenfierString = $page.url.searchParams.get('id');
		if (idenfierString != null) {
			if (idenfierString.startsWith('@')) {
				return {
					type: UserResolveType.Username,
					username: idenfierString.substring(1)
				};
			} else if (idenfierString.startsWith(':')) {
				return {
					type: UserResolveType.UserId,
					userId: Number.parseInt(idenfierString.substring(1))
				};
			} else if (idenfierString.startsWith('!')) {
				return {
					type: UserResolveType.UserId,
					userId: $session!.userId
				};
			}
		}
		return null;
	};

	let userIdentifier: UserResolve | null = $derived(parse());
</script>

{#key userIdentifier}
	{#if userIdentifier != null}
		<ProfilePage identifier={userIdentifier} />
	{:else}
		<pre>
		// TODO: Add all users list
		// Idea:
		//	1. List style with filter options at the top.
	</pre>
	{/if}
{/key}
