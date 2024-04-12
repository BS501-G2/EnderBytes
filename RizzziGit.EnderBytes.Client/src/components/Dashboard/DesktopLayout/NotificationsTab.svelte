<script lang="ts">
  import BackgroundTasks from "../../BackgroundTaskList/BackgroundTaskList.svelte";
  import NavigationTab from "./NavigationTab.svelte";

  export let onDismiss: () => void;
</script>

<NavigationTab {onDismiss}>
  <svelte:fragment slot="head"><p><b>Notifications</b></p></svelte:fragment>
  <svelte:fragment slot="body">
  </svelte:fragment>
</NavigationTab>
