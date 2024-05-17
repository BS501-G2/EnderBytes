import type { AppState } from "./app-state";

export class AppSearchState {
  public constructor(appState: AppState) {
    this.#appState = appState
    this.#string = "";
    this.focused = false;
    this.dismissed = false;

    this.filenameMatches = [];
    this.contentMatches = [];

    this.userMatches = [];
  }

  #appState: AppState

  #string: string;
  get string(): string {
    return this.#string;
  }
  set string(searchString: string) {
    this.#string = searchString;
    this.#execute();
  }

  focused: boolean;
  dismissed: boolean;

  filenameMatches: number[];
  contentMatches: number[];
  userMatches: number[];

  get active(): boolean {
    return this.focused || (!this.dismissed && !!this.string);
  }

  #promise?: Promise<void>;

  get inProgress(): boolean {
    return !!this.#promise;
  }

  #execute() {
    this.#promise ??= (async () => {
      try {
        while (true) {
          let currentSearchString = this.string;

          try {
            await this.#getResults();
          } finally {
            await new Promise((resolve) => setTimeout(resolve, 1000));
          }

          if (this.string == currentSearchString) {
            break;
          }
        }
      } finally {
        this.#promise = undefined;
      }
    })();
  }

  async #getResults(): Promise<void> {
    // await fetch("//localhost:8080/user/auth-password", {
    //  method: "post",

    //  headers: {
    //   "content-type": "application/json",
    //   authorization: "Bearer adasd",
    //  },

    //  body: JSON.stringify({
    //   username: `${Math.random()}`,
    //   password: `${Math.random()}`,
    //  }),
    // });
  }
}
