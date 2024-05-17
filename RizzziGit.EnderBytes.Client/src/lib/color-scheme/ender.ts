import { ColorKey, type ColorValues } from '@rizzzi/svelte-commons';

export const colors = (): ColorValues => ({
  [ColorKey.Primary]: 0x0f3031ff,
  [ColorKey.OnPrimary]: 0xffffffff,
  [ColorKey.PrimaryContainer]: 0x86c24fff,
  [ColorKey.OnPrimaryContainer]: 0x0f3031ff,

  [ColorKey.PrimaryVariant]: 0x0f3031d1,
  [ColorKey.OnPrimaryVariant]: 0xffffffff,
  [ColorKey.PrimaryContainerVariant]: 0xb9dc9aff,
  [ColorKey.OnPrimaryContainierVariant]: 0x37812eff,

  [ColorKey.Background]: 0xebf4e4ff,
  [ColorKey.BackgroundVariant]: 0xffffffff,
  [ColorKey.OnBackground]: 0x1f4041ff,
  [ColorKey.OnBackgroundVariant]: 0x071718ff,

  [ColorKey.Error]: 0xcc3300ff,
  [ColorKey.ErrorBackground]: 0xcc33007f,
  [ColorKey.OnError]: 0xffffffff,

  [ColorKey.Warning]: 0xffcc00ff,
  [ColorKey.WarningBackground]: 0xffcc007f,
  [ColorKey.OnWarning]: 0xffffffff,

  [ColorKey.Info]: 0x40a6ceff,
  [ColorKey.InfoBackground]: 0x40a6ce7f,
  [ColorKey.OnInfo]: 0xffffffff,

  [ColorKey.Shadow]: 0x0000007f
});
