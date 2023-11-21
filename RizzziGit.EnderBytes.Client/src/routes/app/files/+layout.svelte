<script lang="ts">
  import { page } from '$app/stores'
</script>

<slot />
<p>{$page.params.path}</p>