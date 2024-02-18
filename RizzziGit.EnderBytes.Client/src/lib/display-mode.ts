export enum DisplayMode {
  Mobile = 0b0001,
  Desktop = 0b10,

  Browser = 0b100,
  Standalone = 0b10000,
  Fullscreen = 0b100000,

  MobileBrowser = Mobile | Browser,
  DesktopBrowser = Desktop | Browser,

  MobileStandalone = Mobile | Standalone,
  DesktopStandalone = Desktop | Standalone,

  MobileFullscreen = Mobile | Fullscreen,
  DesktopFullscreen = Desktop | Fullscreen
}

export type OnDisplayModeChangeListener = (mode: DisplayMode) => void

let current: DisplayMode = DisplayMode.DesktopBrowser

const onChangeListeners: Array<OnDisplayModeChangeListener> = []

export function addOnDisplayModeChangeListener(onChangeListener: OnDisplayModeChangeListener): boolean {
  if (onChangeListeners.indexOf(onChangeListener) >= 0) {
    return false
  }

  onChangeListeners.push(onChangeListener)
  return true
}

export function removeOnDisplayModeChangeListener(onChangeListener: OnDisplayModeChangeListener): boolean {
  let onChangeListenerIndex: number

  if ((onChangeListenerIndex = onChangeListeners.indexOf(onChangeListener)) == -1) {
    return false
  }

  onChangeListeners.splice(onChangeListenerIndex, 1)
  return true
}

export function triggerUpdateCheck(window?: Window): void {
  if (window == null) {
    return
  }

  if (current != (
    current = window == null
      ? DisplayMode.DesktopBrowser
      : (
        (
          window.matchMedia('(max-width: 720px)').matches
            ? DisplayMode.Mobile
            : DisplayMode.Desktop
        ) |
        (
          window.matchMedia('(display-mode: standalone)').matches ? DisplayMode.Standalone :
            window.matchMedia('(display-mode: fullscreen)').matches ? DisplayMode.Fullscreen : DisplayMode.Browser
        )
      )
  )) {
    for (const handler of onChangeListeners) {
      handler(current)
    }
  }
}

export function getDisplayMode(): DisplayMode { return current }