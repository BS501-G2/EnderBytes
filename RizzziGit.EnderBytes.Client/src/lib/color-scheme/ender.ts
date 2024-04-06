import {
  ColorKey,
  type ColorValues
} from "$lib/color-schemes";

export const colors = (): ColorValues => ({
  [ColorKey.Primary]: 0x0f3031ff,
  [ColorKey.OnPrimary]: 0xffffffff,
  [ColorKey.PrimaryContainer]: 0x86c24fff,
  [ColorKey.OnPrimaryContainer]: 0x0f3031ff,

  [ColorKey.PrimaryVariant]: 0x0f3031ff,
  [ColorKey.OnPrimaryVariant]: 0xff37812e,
  [ColorKey.PrimaryVariantContainer]: 0xffffff7f,
  [ColorKey.OnPrimaryVariantContainer]: 0x37812eff,

  [ColorKey.Background]: 0xebf4e4ff,
  [ColorKey.BackgroundVariant]: 0xffffffff,
  [ColorKey.OnBackground]: 0x0f3031ff
})
