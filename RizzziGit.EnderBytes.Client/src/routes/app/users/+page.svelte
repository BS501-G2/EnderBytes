<script lang="ts">
  import { page } from '$app/stores';
    import { authentication } from '$lib/client/api-functions';

  import ProfilePage, { type UserResolve, UserResolveType } from './profile-page.svelte';

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
      } else if (idenfierString == '!me') {
        return {
          type: UserResolveType.UserId,
          userId: $authentication!.userId
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
    //  1. List style with filter options at the top.
  </pre>
  {/if}
{/key}
