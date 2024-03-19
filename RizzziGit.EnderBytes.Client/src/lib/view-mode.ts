export enum ViewMode {
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
