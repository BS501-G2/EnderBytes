export enum ViewMode {
  Unset = 0,

  Mobile      = 0b00001,
  Desktop     = 0b00010,
  Browser     = 0b00100,
  Standalone  = 0b01000,
  Fullscreen  = 0b10000,

  MobileBrowser = Mobile | Browser,
  DesktopBrowser = Desktop | Browser,

  MobileStandalone = Mobile | Standalone,
  DesktopStandalone = Desktop | Standalone,

  MobileFullscreen = Mobile | Fullscreen,
  DesktopFullscreen = Desktop | Fullscreen
}
