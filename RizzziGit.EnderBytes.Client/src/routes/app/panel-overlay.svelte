<script lang="ts">
	import LoadingParticleSpawner from "../../components/loading/loading-particle-spawner.svelte";
</script>

<style lang="postcss">
  div.overlay-panel {
    width: 100%;
    height: 100%;

    position: absolute;
    
    left: 0px;
    right: 0px;

    pointer-events: none;
  }
</style>

<div class="overlay-panel">
  <LoadingParticleSpawner />
</div>