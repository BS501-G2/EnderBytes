import { derived, writable, type Readable, get, type Writable, readable, type Invalidator } from "svelte/store";

export function followTransition<T extends any>(source: Readable<T>, duration: number, processor: (from: T, to: T) => T) {
  let current: T = structuredClone(get(source))
}
