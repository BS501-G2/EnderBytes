<script lang="ts">
	import { onMount } from "svelte";

  type T = { rotate: number, rotateDirection: number, initialDistance: number, distance: number, opacity: number, distanceDecrement: number, x: number, y: number }
	let particleMap: Map<HTMLDivElement, T[]> = new Map();

	function update() {
    const icons: NodeListOf<HTMLDivElement> = document.querySelectorAll('div.loading-icon')
    
    for (const icon of icons) {
      let particles: T[] | null = particleMap.get(icon) ?? null
      if (particles == null) {
        particleMap.set(icon, particles = [])
      }

      if (Math.floor(Math.random() * 5000) > 4900)
      {
        const distance = 64 + Math.floor(Math.random() * 64)
        const rotate = Math.floor(Math.random() * 360)
        let x = icon.offsetLeft
        let y = icon.offsetTop

        let m: HTMLElement | null = icon.parentElement
        while ((m = m?.parentElement ?? null) != null) {
          x += m.offsetLeft
          y += m.offsetTop
        }

        const rotateDirection = Math.floor(Math.random() * 2) == 0 ? -.5 : 0.5

        particles.push({ distance, rotateDirection, initialDistance: distance, distanceDecrement: 0.1, opacity: 0, rotate, x, y })
      }
    }

    for (const [icon, particles] of particleMap) {
      for (let index = 0; index < particles.length; index++) {
        const particle = particles[index]

        if (particle.distance <= 0) {
          particles.splice(index--, 1)
          continue;
        }

        particle.rotate += particle.rotateDirection

        if ((particle.initialDistance / 2) > particle.distance) {
          particle.opacity = particle.distance / (particle.initialDistance / 2)
        } else {
          particle.opacity = (particle.initialDistance - particle.distance) / (particle.initialDistance / 2)
        }

        particle.distance -= (particle.distanceDecrement += 0.01);
      }

      if (particles.length == 0) {
        particleMap.delete(icon)
      }
    }

    particleMap = particleMap
  }

  function waitFrame() { return new Promise((resolve) => requestAnimationFrame(resolve)) }

  async function runParticleSpawnerUpdate() {
    while (true) {
      update()

      await waitFrame()
    }
  }

  onMount(async () => {
    void runParticleSpawnerUpdate()
  })
</script>

<style lang="postcss">
  div.loading-bounds {
    position: absolute;

    width: 0px;
    height: 0px;

    left: var(--x);
    top: var(--y);

    transform: var(--rotate);
  }

  div.loading-particle {
    width: 16px;
    height: 16px;

    position: absolute;

    background-color: purple;

    left: var(--distance);
    opacity: var(--opacity);
    transform: var(--rotate);
  }
</style>

{#each particleMap.keys() as icon}
{#each particleMap.get(icon) ?? [] as { rotate, opacity, distance, x, y }}
<div class="loading-bounds" style="--rotate:rotate({rotate}deg); --x:{x}px; --y:{y}px; --opacity:{opacity};">
  <div class="loading-particle" style="--rotate:rotate({-rotate}deg); --distance:{Math.floor(distance)}px;"></div>
</div>
{/each}
{/each}