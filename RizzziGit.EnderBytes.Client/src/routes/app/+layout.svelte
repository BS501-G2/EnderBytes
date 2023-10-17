<script lang="ts">
	import { Client, ClientBrowserType, ClientFormFactor } from "$lib/core/client";
	import { onMount } from "svelte";

  import LoadingPage from "../../components/loading-page.svelte";
	import LeftPanel from "./left-panel.svelte";
	import RightPanel from "./right-panel.svelte";
	import OverlayPanel from "./overlay-panel.svelte";

  let loadMessage: string
  let loadPromise: Promise<Client>

  onMount(async () => {
    loadPromise = Client.get({
      browserType: ClientBrowserType.Electron,
      formFactor: ClientFormFactor.Mobile
    })
  })
</script>

<style lang="postcss">
  div.app-container {
    width: 100%;
    height: 100%;

    position: fixed;
    
    top: 0px;
    left: 0px;
  }
</style>

{#await loadPromise}
  <LoadingPage message={loadMessage} />
{:then}
  <div class="app-container">
    <LeftPanel />
    <RightPanel><slot /></RightPanel>
    <OverlayPanel />
  </div>
{/await}