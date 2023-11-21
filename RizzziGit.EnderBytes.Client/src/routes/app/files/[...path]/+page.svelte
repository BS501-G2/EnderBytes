<script lang="ts">
  import { page } from '$app/stores'
	import { Client } from '$lib/core/client';
	import { onMount } from 'svelte';

  type File = {
    path: string
    type: 'file' | 'folder'
  }

  const fakeFiles: File[] = [
    {
      path: 'file.php',
      type: 'file'
    },
    {
      path: 'homework',
      type: 'folder'
    }
  ]
  const currentFakeFile = fakeFiles.find((value) => value.path == $page.params.path as string)

  let client: Client = Client.require()
</script>

{#if currentFakeFile != null}
  <table class="file-table"></table>
{:else}
  <p>Not found</p>
{/if}