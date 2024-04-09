<script lang="ts" context="module">
  export enum UserResolveType {
    Username,
    UserId,
  }
  export type UserResolve =
    | { type: UserResolveType.UserId; userId: number }
    | { type: UserResolveType.Username; username: string };
</script>

<script lang="ts">
  import type { Client } from "$lib/client/client";

  import { RootState } from "$lib/states/root-state";

  export let identifier: UserResolve;

  const rootState = RootState.state;

  let resolved = false;
  let resolvedUser: any | null;

  async function resolve(client: Client): Promise<any | null> {
    if (identifier.type == UserResolveType.Username) {
      const userId = await client.resolveUserId(identifier.username);

      if (userId != null) {
        resolvedUser = await client.getUser(userId);
      }
    } else {
      resolvedUser = await client.getUser(identifier.userId);
    }

    resolved = true;
    return resolvedUser;
  }
</script>

<svelte:head>
  {#if resolvedUser != null}
    {#if identifier.type == UserResolveType.UserId}
      <link rel="canonical" href="@{resolvedUser.Username}" />
    {/if}
  {/if}
</svelte:head>

<div class="user-page">
  {#await $rootState.getClient() then client}
    {#await resolve(client) then user}
      <p>{user.Username}</p>
    {/await}
  {/await}
</div>

<style lang="scss">
  div.user-page {
    width: 100%;
    height: 100%;
  }
</style>
