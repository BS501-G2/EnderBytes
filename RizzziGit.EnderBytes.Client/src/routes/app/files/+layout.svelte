<script lang="ts">
  import { writable } from "svelte/store";

  import { onNavigate } from "$app/navigation";

  import FileBrowser, {
    NormalFileBrowserState,
  } from "../../../components/FileBrowser.svelte";

  const fileBrowserState = writable(new NormalFileBrowserState(null));

  onNavigate((nav) => {
    const currentId = nav.to?.url.pathname.split("/")[3] ?? null;

    $fileBrowserState.fileId =
      currentId != null ? Number.parseInt(currentId) : null;
    $fileBrowserState.selectedFileIds = [];
  });
</script>

<FileBrowser {fileBrowserState} />
<slot/>
