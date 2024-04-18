<script lang="ts">
  import DesktopLayout from "./FileBrowser/DesktopLayout.svelte";
  import ResponsiveLayout from "./Bindings/ResponsiveLayout.svelte";
    import FolderCreationDialog from "./FileBrowser/FolderCreationDialog.svelte";
    import FileCreationDialog from "./FileBrowser/FileCreationDialog.svelte";

  export let currentFileId: number | null;
</script>

<ResponsiveLayout>
  <svelte:fragment slot="desktop">
    <DesktopLayout bind:currentFileId />
  </svelte:fragment>
</ResponsiveLayout>

<FolderCreationDialog bind:currentFileId />
<FileCreationDialog bind:currentFileId />
