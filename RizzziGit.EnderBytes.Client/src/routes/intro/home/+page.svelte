<script lang="ts">
  let scroll: number = 0
</script>

<svelte:window bind:scrollY={scroll}></svelte:window>
<svelte:body style=""></svelte:body>

<p style="position: fixed; left: 0px; top: 0px;">{scroll}</p>
