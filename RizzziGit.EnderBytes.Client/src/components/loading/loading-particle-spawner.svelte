<script lang="ts">
	import { onMount } from "svelte";

  type T = { rotate: number, rotateDirection: number, initialDistance: number, particle: number, distance: number, opacity: number, distanceDecrement: number, x: number, y: number }
	let particleMap: Map<HTMLDivElement, T[]> = new Map();

  function random (min: number, max: number) {
    return min + (Math.floor(Math.random() * max))
  }

  function updateNodeList() {
    const icons: NodeListOf<HTMLDivElement> = document.querySelectorAll('div.loading-icon')

    if (icons.length != 0) {
      for (const icon of icons) {
        let particles: T[] | null = particleMap.get(icon) ?? null
        if (particles == null) {
          particleMap.set(icon, particles = [])
        }

        if (random(0, 5000) < 5000)
        {
          const distance = random(64, 128)
          const rotate = random(0, 360)
          let x = icon.offsetLeft + (icon.clientWidth / 2)
          let y = icon.offsetTop + (icon.clientHeight / 2)

          let m: HTMLElement | null = icon.parentElement
          while ((m = m?.parentElement ?? null) != null) {
            x += m.offsetLeft
            y += m.offsetTop
          }

          const rotateDirection = random(0, 2) == 0 ? -1 : 1
          const particle = random(0, 8)

          particles.push({ distance, rotateDirection, particle, initialDistance: distance, distanceDecrement: 0.1, opacity: 0, rotate, x, y })
        }
      }
    }
  }

	function updateParticles() {
    for (const [icon, particles] of particleMap) {
      for (let index = 0; index < particles.length; index++) {
        const particle = particles[index]

        if (particle.distance <= 0) {
          particles.splice(index--, 1)
          continue;
        }

        particle.rotate += particle.rotateDirection
        particle.opacity = (particle.initialDistance / 2) > particle.distance
          ? particle.distance / (particle.initialDistance / 2)
          : (particle.initialDistance - particle.distance) / (particle.initialDistance / 2)
        particle.distance -= ((particle.distanceDecrement) += 0.05) * 2;
      }

      if (particles.length == 0) {
        particleMap.delete(icon)
      }
    }

    particleMap = particleMap
  }

  function wait(time: number) { return new Promise((resolve) => setTimeout(resolve, time)) }

  async function runNodeUpdate () {
    while (true) {
      if (document.hasFocus()) {
        updateNodeList()
      }

      await wait(100)
    }
  }

  async function runParticleUpdate () {
    while (true) {
      if (document.hasFocus()) {
        updateParticles()
      }

      await wait((1/30) * 1000)
    }
  }

  onMount(async () => {
    void runNodeUpdate()
    void runParticleUpdate()
  })
</script>

<style lang="postcss">
  div.loading-particle {
  }

  div.loading-particle-bounds {
    position: absolute;

    width: 0px;
    height: 0px;

    left: var(--x);
    top: var(--y);

    transform: var(--rotate);
    transition: transform 500;
  }

  div.loading-particle-sprite {
    width: 16px;
    height: 16px;

    position: absolute;

    background-image: var(--background-image);
    filter: sepia(100%) saturate(1000%) brightness(50%) hue-rotate(200deg);
    background-repeat: no-repeat;
    background-size: contain;
    image-rendering: pixelated;

    left: var(--distance);
    opacity: var(--opacity);
    transform: var(--rotate);
  }
</style>

{#each particleMap.keys() as icon}
{#each particleMap.get(icon) ?? [] as { rotate, opacity, distance, particle, x, y }}
<div class="loading-particle">
  <div class="loading-particle-bounds" style="--rotate:rotate({rotate}deg); --background-image:url(/images/particles/generic_{particle}.png); --x:{x}px; --y:{y}px; --opacity:{opacity};">
    <div class="loading-particle-sprite" style="--rotate:rotate({-(rotate*2)}deg); --distance:{Math.floor(distance)}px;">
      <!-- particle x:{x}px y:{y}px opacity:{Math.floor(opacity*10)/10}px distance:{Math.floor(distance)}px rotate:{Math.floor(rotate)}deg -->
    </div>
  </div>
</div>
{/each}
{/each}