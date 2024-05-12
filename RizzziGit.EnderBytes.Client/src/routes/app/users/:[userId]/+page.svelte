<script lang="ts">
	import { page } from '$app/stores';

	import ProfilePage, { UserResolveType } from '../profile-page.svelte';
</script>

<ProfilePage
	identifier={{
		type: UserResolveType.UserId,
		userId: Number.parseInt($page.params.userId)
	}}
/>
