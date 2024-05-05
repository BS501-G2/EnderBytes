<script lang="ts">
	import { page } from '$app/stores';

	import ProfilePage, { UserResolveType } from '../../../../components/ProfilePage.svelte';
</script>

<ProfilePage
	identifier={{
		type: UserResolveType.Username,
		username: $page.params.username
	}}
/>
