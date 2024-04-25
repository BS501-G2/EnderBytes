<script lang="ts" context="module">
  import { get, writable, type Writable } from "svelte/store";

  export interface Session {
    userId: number;
    token: string;
  }

  export type FetchFunction = (
    pathname: string,
    method?: string,
    data?: Blob | any,
    headers?: Record<string, string>,
  ) => Promise<Response>;

  export type FetchAndInterpretFunction = (
    ...args: Parameters<FetchFunction>
  ) => Promise<Blob | any>;

  export type ApiFetchFunction = (
    ...args: Parameters<FetchAndInterpretFunction>
  ) => Promise<Blob | any>;

  export function getApiUrl(path: string = "/"): URL {
    let url = localStorage.getItem("client-url");

    if (url == null) {
      localStorage.setItem("client-url", (url = "http://25.22.231.71:8083/"));
    }

    return Object.assign(new URL(url), {
      pathname: path,
    });
  }

  const sessionStore: Writable<Session | null> = writable(
    JSON.parse(localStorage.getItem("session") ?? "null"),
  );
  sessionStore.subscribe((value) =>
    localStorage.setItem("session", JSON.stringify(value)),
  );

  export class ClientError extends Error {
    public constructor(response: Response, message?: string) {
      super(message ?? `Server Response: ${response.status} ${response.statusText}`);

      this.#response = response;
    }

    #response: Response;
    public get response() {
      return this.#response;
    }

    public interpret(): Promise<Blob | any> {
      return interpretResponse(this.response);
    }
  }

  export async function interpretResponse(
    response: Response,
  ): Promise<Blob | any> {
    const responseType = response.headers.get("Content-Type");

    if (responseType != null && responseType.startsWith("application/json")) {
      return await response.json();
    } else {
      return await response.blob();
    }
  }

  const oldFetch = window.fetch;
  export const fetch: FetchFunction = async function fetch(
    pathname: string,
    method: string = "GET",
    data?: Blob | any,
    headers?: Record<string, string>,
  ): Promise<Response> {
    const session = get(sessionStore);
    const request: RequestInit = {};

    request.headers = structuredClone(headers ?? {});
    if (session != null) {
      request.headers["Authorization"] =
        `Basic ${btoa(JSON.stringify(session))}`;
    }

    if (data != null) {
      if (data instanceof Blob) {
        request.body = new Blob([data], { type: "application/octet-stream" });
      } else {
        request.body = new Blob([JSON.stringify(data)], {
          type: "application/json",
        });
      }
    }
    request.method = method;

    const url = Object.assign(getApiUrl(), {
      pathname: pathname,
    });

    const response = await oldFetch(url, request);

    if (response.status === 200) {
      if (pathname === "/auth/password-login" && request.method === "POST") {
        const session: Session = (await response.json()).data;

        Object.assign(response, { json: () => session });

        sessionStore.set(session);
      } else if (pathname === "/auth/logout" && request.method === "POST") {
        sessionStore.set(null);
      } else {
        return response;
      }
    } else if (response.status === 401) {
      sessionStore.set(null);

      Object.assign(location, {
        pathname: "/app/auth/login",
      });
    }

    if (response.status >= 200 && response.status <= 300) {
      return response;
    } else {
      let responseData: any;
      try {
        responseData = await response.json();
      } catch {}

      if (responseData != null && "error" in responseData) {
        const { error } = responseData;

        throw new ClientError(error, `${error.name}: ${error.message}`);
      } else {
        throw new ClientError(response);
      }
    }
  };

  export const fetchAndInterpret: FetchAndInterpretFunction = async (...args) =>
    interpretResponse(await fetch(...args));

  export const apiFetch: ApiFetchFunction = async (...args) => {
    const response = await fetchAndInterpret(...args);

    if (response instanceof Blob) {
      return response;
    } else {
      return response.data;
    }
  };

  export { sessionStore as session };

  Object.assign(window, {
    fetch,
    fetchAndInterpret,
  });
</script>

<script lang="ts">
  interface $$Slots {
    default: {
      fetch: FetchFunction;
      fetchAndInterpret: FetchAndInterpretFunction;
      apiFetch: ApiFetchFunction;
      session: Session | null;
    };
  }
</script>

<slot {fetch} {fetchAndInterpret} {apiFetch} session={$sessionStore} />
