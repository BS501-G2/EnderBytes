import type { AppState } from "./app-state";

export class AppFileState {
  public constructor (appState: AppState) {
    this.#appState = appState

    this.clipboard = null
    this.currentFileId = null
  }

  #appState: AppState

  clipboard: Clipboard | null
  currentFileId: number | null
}

export enum ClipboardIntent {
  Copy, Cut
}

export interface Clipboard {
  targetIds: number[]

  intent: ClipboardIntent
}
