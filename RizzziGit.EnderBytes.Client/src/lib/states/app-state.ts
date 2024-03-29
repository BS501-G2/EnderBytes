import { AppSearchState } from "$lib/states/app-search-state";
import { writable, type Writable } from "svelte/store";

export class AppState {
  public constructor() {
    this.appInfoShown = false;

    this.searchState = writable(new AppSearchState());
  }

  appInfoShown: boolean;

  searchState: Writable<AppSearchState>;
}
