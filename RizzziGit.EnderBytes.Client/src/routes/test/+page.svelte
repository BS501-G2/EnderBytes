<script lang="ts" context="module">
  import { Client } from "$lib/client/client";
</script>

{#await Client.getInstance(new URL("ws://localhost:8080/ws")) then client}
  <p>{#await client.test() then a}{a}{/await}</p>
{/await}
