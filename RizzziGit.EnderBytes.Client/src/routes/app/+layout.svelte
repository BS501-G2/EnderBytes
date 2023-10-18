<script lang="ts">
	import { Client, ClientBrowserType, ClientFormFactor } from "$lib/core/client";
	import { onMount } from "svelte";

  import LoadingPage from "../../components/loading-page.svelte";
	import PanelSideDesktop from "./panel-side-desktop.svelte";
	import PanelMain from "./panel-main.svelte";
	import PanelOverlay from "./panel-overlay.svelte";

  import { FileIcon, HomeIcon, ImageIcon, SettingsIcon, StarIcon, TrashIcon, UsersIcon } from 'svelte-feather-icons'

  let client: Client | undefined = undefined

  let paths: [name: string, href: string, typeof ImageIcon][] = [
    ['Admin', `/app/admin`, SettingsIcon],
    ['Dashboard', '/app/dashboard', HomeIcon],
    ['My Files', '/app/files', FileIcon],
    ['Favorites', '/app/favorites', StarIcon],
    ['Shared', '/app/shared', UsersIcon],
    ['Trash', '/app/trash', TrashIcon]
  ]

  onMount(async () => {
    client = await Client.get({
      browserType: ClientBrowserType.Electron,
      formFactor: navigator.userAgent.includes('Mobi')
        ? ClientFormFactor.Mobile
        : ClientFormFactor.PC
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

<title>EnderBytes</title>

{#if client == null}
  <LoadingPage message="LOADING" />
{:else}
  <div class="app-container">
    <PanelMain ><slot /></PanelMain>
    {#if client.configuration.formFactor == ClientFormFactor.PC}
    <PanelSideDesktop {paths} />
    {/if}
    <PanelOverlay />
  </div>
{/if}