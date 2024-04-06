<script>
  import { page } from '$app/stores'

  import Page from "../+page.svelte";
</script>

<Page initialId={Number.parseInt($page.url.pathname.split('/')[3]) || null}></Page>
