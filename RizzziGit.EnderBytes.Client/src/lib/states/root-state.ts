import { ColorScheme, type ColorKey, type ColorValues, colors } from "$lib/color-schemes";
import { Locale, LocaleKey, getClientResponseString, getString } from "$lib/locale";
import { ViewMode } from "$lib/view-mode";
import { writable, type Writable } from "svelte/store";
import { AppState } from "./app-state";
import { Client, type Session } from "$lib/client/client";
import { KeyboardState } from "../../components/Bindings/Keyboard.svelte";
import { AwaiterState } from "../../components/Bindings/Awaiter.svelte";

export class RootState {
  static #state?: Writable<RootState>

  public static get state(): Writable<RootState> {
    const state = this.#state ??= writable(new this())

    let capturedSessionToken: Session | null = null

    state.subscribe((value) => {
      if (capturedSessionToken != value.sessionToken) {
        value.appState.set(new AppState())

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

    this.appState = writable(new AppState())
    this.keyboardState = writable(new KeyboardState())
    this.awaiterState = writable(new AwaiterState())
  }

  theme: ColorScheme;
  viewMode: ViewMode;
  locale: Locale;
  connectionState: number;

  sessionToken: Session | null
  appState: Writable<AppState>
  keyboardState: Writable<KeyboardState>
  awaiterState: Writable<AwaiterState>

  public get isDesktop(): boolean { return !!(this.viewMode & ViewMode.Desktop) }
  public get isMobile(): boolean { return !!(this.viewMode & ViewMode.Mobile) }

  public async getClient(): Promise<Client> {
    const client = await Client.getInstance(new URL('ws://25.20.99.238:8083/'), {
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
    return `#${this.getColor(key).toString(16).padStart(8, '0') ?? 'transparent'}`
  }

  public getString<T extends LocaleKey>(
    key: T,
    params?: Record<string, string>,
  ) {
    return getString(this.locale, key, params);
  }

  public getClientResponseString(responseCodeString: string): string {
    return getClientResponseString(this.locale, responseCodeString)
  }
}
