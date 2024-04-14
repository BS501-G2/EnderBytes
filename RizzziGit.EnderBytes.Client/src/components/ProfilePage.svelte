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
  import Awaiter from "./Bindings/Awaiter.svelte";
  import ClientAwaiter from "./Bindings/ClientAwaiter.svelte";

  export let identifier: UserResolve;
  let userPromise: Promise<any> | null;

  async function resolve(client: Client): Promise<any | null> {
    if (identifier.type == UserResolveType.Username) {
      const userId = await client.resolveUserId(identifier.username);

      if (userId != null) {
        return await client.getUser(userId);
      }
    } else {
      return await client.getUser(identifier.userId);
    }
  }
</script>

<svelte:head>
  {#if identifier.type == UserResolveType.UserId}
    {#key userPromise}
      <Awaiter callback={() => userPromise}>
        <svelte:fragment slot="success" let:result={user}>
          <link rel="canonical" href="@{user.Username}" />
        </svelte:fragment>
      </Awaiter>
    {/key}
  {/if}
</svelte:head>

<div class="user-page">
  <ClientAwaiter>
    <svelte:fragment let:client>
      <Awaiter callback={() => (userPromise = resolve(client))}>
        <svelte:fragment slot="success" let:result={user}>
          // TODO: Add user page arvin dito
          <div>
              <div class="top">
                
              </div>
              <div class="bottom">

              </div>
          </div>
          <p>
            {user.LastName}, {user.FirstName}
            {user.MiddleName ? `${user.MiddleName[0]}.` : ""} (@{user.Username})
          </p>
        </svelte:fragment>
      </Awaiter>
    </svelte:fragment>
  </ClientAwaiter>
</div>

<style lang="scss">
  div.user-page {
    width: 100%;
    height: 100%;
  }

</style>
