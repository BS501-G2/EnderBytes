import { ColorScheme, type ColorKey, type ColorValues, colors } from "$lib/color-schemes";
import { Locale, LocaleKey, getString } from "$lib/locale";
import { ViewMode } from "$lib/view-mode";
import { writable, type Writable } from "svelte/store";
import { AppState } from "./app-state";
import { Client, type Session } from "$lib/client/client";

export class RootState {
  static #state?: Writable<RootState>

  public static get state(): Writable<RootState> {
    const state = this.#state ??= writable(new this())

    let capturedSessionToken: Session | null = null

    state.subscribe((value) => {
      if (capturedSessionToken != value.sessionToken) {
        value.appState.set(new AppState(value))

        capturedSessionToken = value.sessionToken
      }
    })

    return state
  }

  public constructor() {
    this.theme = ColorScheme.Ender;
    this.viewMode = ViewMode.Unset;
    this.locale = Locale.en_US;
    this.sessionToken = null;
    this.connectionState = Client.STATE_NOT_CONNECTED;

    this.appState = writable(new AppState(this))
  }

  theme: ColorScheme;
  viewMode: ViewMode;
  locale: Locale;
  connectionState: number;

  sessionToken: Session | null
  appState: Writable<AppState>

  public async getClient(): Promise<Client> {
    const client = await Client.getInstance(new URL('ws://10.1.0.117:8083'), {
      stateChange: (state) => RootState.state.update((value) => {
        value.connectionState = state

        return value
      }),
      sessionChange: (sessionToken) => RootState.state.update((value) => {
        value.sessionToken = sessionToken ?? null

        return value
      })
    })

    return client
  }

  public getColor<T extends ColorKey>(key: T): ColorValues[T] {
    return colors[this.theme][key]
  }

  public getColorHex<T extends ColorKey>(key: T): string {
    return `${this.getColor(key).toString(16) ?? 'transparent'}`
  }

  public getString<T extends LocaleKey>(
    key: T,
    params?: Record<string, string>,
  ) {
    return getString(this.locale, key, params);
  }
}
