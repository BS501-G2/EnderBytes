<script lang="ts" context="module">
  export interface KeyboardListener {
    keys: string[];

    func: () => Promise<void> | void;
  }

  let keys: string[] = [];
  let instances: WeakRef<(type: "up" | "down", keys: string[]) => boolean>[] = [];

  export class KeyboardState {
    public constructor() {
      this.#listeners = [];

      instances.push(new WeakRef(() => this.#trigger()));
    }

    #listeners: KeyboardListener[];

    #trigger(): boolean {
      let prevent = false;

      for (const listener of this.#listeners) {
        if (listener.keys.every((key) => keys.includes(key))) {
          prevent = true;

          void listener.func();
        }
      }

      return prevent;
    }

    addListener(keys: string[], func: () => void): () => void {
      const a = { keys, func }

      this.#listeners.push(a);

      return () => {
        const index = this.#listeners.indexOf(a);

        this.#listeners.splice(index, 1);
      }
    }

    hasKeys(...findKeys: string[]): boolean {
      return findKeys.every((key) => keys.includes(key));
    }
  }
</script>

<script lang="ts">
  function onKey(type: "up" | "down", event: KeyboardEvent) {
    const key = event.key?.toLowerCase() ?? '';

    if (type == "down" && !keys.includes(key)) {
      keys.push(key);
    } else if (type == "up" && keys.includes(key)) {
      const index = keys.indexOf(key);

      keys.splice(index, 1);
    }

    if (instances.some((ref) => ref.deref()?.(type, keys))) {
      event.preventDefault();
    }
  }
</script>

<svelte:window
  on:keydown={(event) => onKey("down", event)}
  on:keyup={(event) => onKey("up", event)}
/>
