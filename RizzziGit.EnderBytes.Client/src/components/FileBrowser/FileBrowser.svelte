<script lang="ts">
  import DesktopLayout from "./DesktopLayout.svelte";
  import ResponsiveLayout from "../Bindings/ResponsiveLayout.svelte";

  export let currentFileId: number | null;
</script>

<ResponsiveLayout>
  <svelte:fragment slot="desktop">
    <DesktopLayout bind:currentFileId />
  </svelte:fragment>
</ResponsiveLayout>
