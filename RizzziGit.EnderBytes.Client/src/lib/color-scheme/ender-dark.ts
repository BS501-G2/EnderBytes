import { ColorKey, type ColorValues } from '@rizzzi/svelte-commons'
import { colors as base } from './ender'

export const colors = (): ColorValues => {
  const baseColors = base()

  return ({
    [ColorKey.Primary]: baseColors[ColorKey.PrimaryContainer],
    [ColorKey.OnPrimary]: baseColors[ColorKey.OnPrimaryContainer],
    [ColorKey.PrimaryContainer]: baseColors[ColorKey.Primary],
    [ColorKey.OnPrimaryContainer]: baseColors[ColorKey.OnPrimary],

    [ColorKey.PrimaryVariant]: baseColors[ColorKey.PrimaryContainerVariant],
    [ColorKey.OnPrimaryVariant]: baseColors[ColorKey.OnPrimaryContainierVariant],
    [ColorKey.PrimaryContainerVariant]: baseColors[ColorKey.PrimaryVariant],
    [ColorKey.OnPrimaryContainierVariant]: baseColors[ColorKey.OnPrimaryVariant],

    [ColorKey.Background]: baseColors[ColorKey.OnBackground],
    [ColorKey.BackgroundVariant]: baseColors[ColorKey.OnBackgroundVariant],
    [ColorKey.OnBackground]: baseColors[ColorKey.Background],
    [ColorKey.OnBackgroundVariant]: baseColors[ColorKey.BackgroundVariant],

    [ColorKey.Error]: baseColors[ColorKey.Error],
    [ColorKey.ErrorBackground]: baseColors[ColorKey.ErrorBackground],
    [ColorKey.OnError]: baseColors[ColorKey.OnError],

    [ColorKey.Warning]: baseColors[ColorKey.Warning],
    [ColorKey.WarningBackground]: baseColors[ColorKey.WarningBackground],
    [ColorKey.OnWarning]: baseColors[ColorKey.OnWarning],

    [ColorKey.Info]: baseColors[ColorKey.Info],
    [ColorKey.InfoBackground]: baseColors[ColorKey.InfoBackground],
    [ColorKey.OnInfo]: baseColors[ColorKey.OnInfo],

    [ColorKey.Shadow]: 0x0000007f
  })
}
