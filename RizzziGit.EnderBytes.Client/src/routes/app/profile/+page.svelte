<script lang="ts">
  import { goto } from "$app/navigation";
  import { RootState } from "$lib/states/root-state";
  import { onMount } from "svelte";

  const rootState = RootState.state;

  onMount(async () => {
    const client = await $rootState.getClient();

    goto(`profile/:${client.session!.userId}`);
  });
</script>
