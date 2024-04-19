<script lang="ts">
  import ProfilePage, {
    UserResolveType,
  } from "../../../../components/ProfilePage.svelte";
  import { session } from "../../../../components/Bindings/Client.svelte";

  $: userId = $session!.userId;
</script>

<ProfilePage
  identifier={{
    type: UserResolveType.UserId,
    userId,
  }}
/>
