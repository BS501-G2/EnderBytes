<script lang="ts">
  import { goto } from '$app/navigation'

  import { session } from "../../../components/Bindings/Client.svelte";

  $: {
    if ($session != null) {
      goto('/app', { replaceState: true })
    }
  }
</script>

<slot/>
