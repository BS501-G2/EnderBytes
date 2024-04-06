import { get, writable, type Writable } from "svelte/store";

import { LocaleKey } from "$lib/locale";
import { AppSearchState } from "$lib/states/app-search-state";
import type { RootState } from "$lib/states/root-state";
import { AppFileState } from "./app-file-state";

export class AppState {
  public constructor() {
    this.searchState = writable(new AppSearchState(this));
    this.fileState = writable(new AppFileState(this));
  }

  searchState: Writable<AppSearchState>;
  fileState: Writable<AppFileState>;
}
