<script lang="ts">
  import { page } from "$app/stores";
  import { RootState } from "$lib/states/root-state";

  import FileBrowser from "../../../components/FileBrowser/FileBrowser.svelte";
  import Loader from "../../../components/Bindings/Awaiter.svelte";

  const rootState = RootState.state;

  function getCurrentId(url: URL) {
    const currentId = url.pathname.split("/")[3] ?? null;

    return (currentId != null ? Number.parseInt(currentId) : null) ?? null;
  }
</script>

<Loader callback={() => $rootState.getClient()}>
  <svelte:fragment slot="success" let:result={client}>
    <FileBrowser {client} currentFileId={getCurrentId($page.url)} />
  </svelte:fragment>
</Loader>
