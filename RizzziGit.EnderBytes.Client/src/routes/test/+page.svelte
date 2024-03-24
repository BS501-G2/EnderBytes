<script lang="ts" context="module">
  import { Client } from "$lib/client/client";

</script>

{#await Client.getInstance(new URL("ws://localhost:8080/ws"))}
<p>Loading...</p>
{:then client}
<p>Loaded.</p>
  <!-- <p>{#await client then a}{a}{/await}</p> -->
{/await}
