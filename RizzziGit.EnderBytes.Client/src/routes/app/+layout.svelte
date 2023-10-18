<script lang="ts">
	import { Client, ClientBrowserType, ClientFormFactor } from "$lib/core/client";
	import { onMount } from "svelte";

  import LoadingPage from "../../components/loading-page.svelte";
	import PanelSide from "./panel-side.svelte";
	import PanelMain from "./panel-main.svelte";
	import PanelOverlay from "./panel-overlay.svelte";

  let loadMessage: string
  let loadPromise: Promise<Client>

  onMount(async () => {
    loadPromise = Client.get({
      browserType: ClientBrowserType.Electron,
      formFactor: screen.orientation.angle != null
        ? ClientFormFactor.Mobile
        : ClientFormFactor.PC
    })

    console.log((await loadPromise).configuration.formFactor)
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
    <PanelOverlay />
    <PanelMain><slot /></PanelMain>
    <PanelSide />
  </div>
{/await}