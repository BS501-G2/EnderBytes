<script lang="ts">
  import { onMount } from "svelte";

  import type { Client } from "$lib/client/client";

  import Input from "../../Widgets/Input.svelte";
  import CreationForm from "./CreationForm.svelte";
  import { goto } from "$app/navigation";
  import { executeBackgroundTask } from "../../BackgroundTaskList/BackgroundTaskList.svelte";

  export let client: Client;
  export let folderId: number | null;
  export let onSubmit: () => Promise<number> | number;

  let name: string;

  onMount(() => {
    onSubmit = async () => {
      const task = executeBackgroundTask(
        "Folder Creation",
        true,
        async (_, setStatus) => {
          setStatus("Creating new folder...");
          const id = await client.createFolder(name, folderId);
          setStatus("Redirecting");
          goto(`/app/files/${id}`);

          return id
        },
      );

      return <number> await task.run();
    };
  });
</script>

<CreationForm>
  <svelte:fragment slot="header">Create a new folder.</svelte:fragment>
  <svelte:fragment slot="sub-header">
    New folder will be created under the current folder.
  </svelte:fragment>
  <svelte:fragment slot="body">
    <Input
      name="Name"
      placeholder={"New folder name"}
      type="text"
      bind:text={name}
    />
  </svelte:fragment>
</CreationForm>
