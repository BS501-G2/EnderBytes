<script lang="ts" context="module">
  import { onDestroy, onMount } from "svelte";
  import { get, writable, type Writable } from "svelte/store";

  export type FrameCallback = (
    previousTime: number | null,
    time: number,
  ) => void;

  export interface FrameListener {
    previousTime: number | null;

    callback: FrameCallback;
  }

  let listeners: Writable<FrameListener[]> = writable([]);
  let running: Writable<boolean> = writable(false);

  listeners.subscribe((updatedValue) => {
    const update = () => {
      if (!get(running)) {
        return;
      }

      const cachedListeners = get(listeners);

      if (!cachedListeners.length) {
        running.set(false);
      }

      for (const listener of cachedListeners) {
        try {
          listener.callback(
            listener.previousTime,
            (listener.previousTime = Date.now()),
          );
        } catch (error: any) {
          console.error(error);
        }
      }

      requestAnimationFrame(update);
    };

    if (updatedValue.length !== 0 && !get(running)) {
      running.set(true);
      requestAnimationFrame(update);
    }
  });
</script>

<script lang="ts">
  export let callback: FrameCallback;

  const frameListener: FrameListener = {
    previousTime: null,
    callback: (previousTime, time) =>
      callback?.(previousTime, time),
  };

  onMount(() => {
    listeners.update((value) => {
      value.push(frameListener);

      return value;
    });
  });

  onDestroy(() => {
    listeners.update((value) => {
      const index = value.indexOf(frameListener);

      if (index >= 0) {
        value.splice(index, 1);
      }

      return value;
    });
  });
</script>

<slot />
