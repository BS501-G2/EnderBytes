<script lang="ts">
  import { goto } from "$app/navigation";
  import { page } from "$app/stores";
  import { onMount } from "svelte";

  const path = $page.url.pathname;

  onMount(() => {
    if (path.startsWith("/app")) {
      goto(`/app?invalid-path=${encodeURIComponent(path)}`, {
        replaceState: true,
      });
    }

    goto(`/home?invalid-path=${encodeURIComponent(path)}`, {
      replaceState: true,
    });
  });
</script>

<p>error</p>