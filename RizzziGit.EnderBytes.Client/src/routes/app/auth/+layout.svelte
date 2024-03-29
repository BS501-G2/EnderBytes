<script lang="ts">
  import { goto } from '$app/navigation'

  import { RootState } from '$lib/states/root-state'

  const rootState = RootState.state

  $: {
    if ($rootState.sessionToken != null) {
      goto('/app', { replaceState: true })
    }
  }
</script>

<slot/>
