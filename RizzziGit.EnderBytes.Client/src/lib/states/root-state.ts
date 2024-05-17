import { writable, type Writable } from "svelte/store";
import { AppState } from "./app-state";

export class RootState {
  static #state?: Writable<RootState>

  public static get state(): Writable<RootState> {
    const state = this.#state ??= writable(new this())

    return state
  }

  public constructor() {
    this.appState = writable(new AppState())
  }
  appState: Writable<AppState>
}
