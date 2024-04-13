<script lang="ts">
  import DesktopLayout from "./DesktopLayout.svelte";
  import type { Client } from "$lib/client/client";
  import ResponsiveLayout from "../Bindings/ResponsiveLayout.svelte";

  export let client: Client;
  export let currentFileId: number | null;
</script>

<ResponsiveLayout>
  <svelte:fragment slot="desktop">
    <DesktopLayout {client} bind:currentFileId />
  </svelte:fragment>
</ResponsiveLayout>
