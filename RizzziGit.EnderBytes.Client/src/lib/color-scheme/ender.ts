import {
  THEME_COLOR_BACKGROUND,
  THEME_COLOR_ON_BACKGROUND,
  THEME_COLOR_ON_PRIMARY,
  THEME_COLOR_ON_PRIMARY_CONTAINER,
  THEME_COLOR_PRIMARY,
  THEME_COLOR_PRIMARY_CONTAINER,
  THEME_COLOR_PRIMARY_VARIANT,
  THEME_COLOR_ON_PRIMARY_VARIANT,
  THEME_COLOR_ON_PRIMARY_VARIANT_CONTAINER,
  THEME_COLOR_PRIMARY_VARIANT_CONTAINER,

  type ColorValues
} from "$lib/color-schemes";

export const colors = (): ColorValues => ({
  [THEME_COLOR_PRIMARY]: '#37812e',
  [THEME_COLOR_ON_PRIMARY]: '#ffffffff',
  [THEME_COLOR_PRIMARY_CONTAINER]: '#86c058',
  [THEME_COLOR_ON_PRIMARY_CONTAINER]: '#ffffffff',

  [THEME_COLOR_PRIMARY_VARIANT]: '#ffffff7f',
  [THEME_COLOR_ON_PRIMARY_VARIANT]: '#37812eff',
  [THEME_COLOR_PRIMARY_VARIANT_CONTAINER]: '#ffffff7f',
  [THEME_COLOR_ON_PRIMARY_VARIANT_CONTAINER]: '#37812eff',

  [THEME_COLOR_BACKGROUND]: '#86c05852',
  [THEME_COLOR_ON_BACKGROUND]: '#000000ff'
})
