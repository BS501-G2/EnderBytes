export class IntroNavigationEntry {
  public constructor(name: string, path: string) {
    this.name = name
    this.path = path
  }

  public readonly name: string
  public readonly path: string
}

export const introNavigationEntries: IntroNavigationEntry[] = [
  new IntroNavigationEntry("Home", "#home"),
  new IntroNavigationEntry("Contact", "#contact"),
  new IntroNavigationEntry("About", "#about"),
  new IntroNavigationEntry("Documentation", "/docs")
]

export const introNavigationButtons: IntroNavigationEntry[] = [
  new IntroNavigationEntry("Go To Dashboard", "../app"),
  new IntroNavigationEntry("Download", "#download")
]
