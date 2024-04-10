import { writable, type Writable } from "svelte/store";

import { AppSearchState } from "$lib/states/app-search-state";

export class AppState {
  public constructor() {
    this.searchState = writable(new AppSearchState(this));
  }

  searchState: Writable<AppSearchState>;
}
