<script lang="ts">
  export let message: string
</script>

<h1>Loading...</h1>
<p bind:textContent={message} contenteditable="false"></p>