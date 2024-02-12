export enum Theme {
  Default = 'default',
  Blue = 'blue'
}

export type OnThemeChangeListener = (theme: Theme) => void


const KEY_THEME: string = 'theme-name'
const onChangeListeners: Array<OnThemeChangeListener> = []

export function addOnThemeChangeListener(onChangeListener: OnThemeChangeListener): boolean {
  if (onChangeListeners.indexOf(onChangeListener) >= 0) {
    return false
  }

  onChangeListeners.push(onChangeListener)
  return true
}

export function removeOnThemeChangeListener(onChangeListener: OnThemeChangeListener): boolean {
  let onChangeListenerIndex: number

  if ((onChangeListenerIndex = onChangeListeners.indexOf(onChangeListener)) == -1) {
    return false
  }

  onChangeListeners.splice(onChangeListenerIndex, 1)
  return true
}

export function getTheme(window: Window): Theme {
  return (<Theme | null>(window.localStorage.getItem(KEY_THEME))) ?? Theme.Default
}

export function setTheme(window: Window, theme: Theme): void {
  window.localStorage.setItem(KEY_THEME, theme)

  for (const onChangeListener of onChangeListeners) {
    onChangeListener(theme)
  }
}