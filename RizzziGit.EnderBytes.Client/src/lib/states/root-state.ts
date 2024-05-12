import { ColorScheme, type ColorKey, type ColorValues, colors } from "$lib/color-schemes";
import { writable, type Writable } from "svelte/store";
import { AppState } from "./app-state";

export class RootState {
	static #state?: Writable<RootState>

	public static get state(): Writable<RootState> {
		const state = this.#state ??= writable(new this())

		return state
	}

	public constructor() {
		this.theme = ColorScheme.Ender;

		this.appState = writable(new AppState())
	}

	theme: ColorScheme;

	appState: Writable<AppState>

	public getColor<T extends ColorKey>(key: T): ColorValues[T] {
		return colors[this.theme][key]
	}

	public getColorHex<T extends ColorKey>(key: T): string {
		return `#${this.getColor(key).toString(16).padStart(8, '0') ?? 'transparent'}`
	}
}
