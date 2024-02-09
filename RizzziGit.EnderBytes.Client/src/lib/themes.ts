export enum ThemeType {
  Default = 'default'
}

export class ThemeManager {
  constructor() {
    throw new Error('Cannot be instantiated.')
  }

  static #KEY_THEME: string = 'theme-name'

  public static get(storage: Storage): ThemeType {
    return (<ThemeType | null>storage.getItem(this.#KEY_THEME)) ?? ThemeType.Default
  }

  public static set(storage: Storage, theme: ThemeType): void {
    storage.setItem(this.#KEY_THEME, theme)
  }
}