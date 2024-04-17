<script lang="ts" context="module">
  import { get, writable, type Writable } from "svelte/store";

  export interface Session {
    userId: number;
    token: string;
  }

  function getUrl() {
    let url = localStorage.getItem("client-url");

    if (url == null) {
      localStorage.setItem("client-url", (url = "http://25.20.99.238:8083/"));
    }

    return new URL(url);
  }

  const sessionStore: Writable<Session | null> = writable(
    JSON.parse(localStorage.getItem("session") ?? "null"),
  );
  sessionStore.subscribe((value) =>
    localStorage.setItem("session", JSON.stringify(value)),
  );

  export class ClientError extends Error {
    public constructor(response: Response) {
      super("Server response: " + response.statusText);

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

  export async function interpretResponse(response: Response): Promise<Blob | any> {
    const responseType = response.headers.get("Content-Type");

    if (responseType != null && responseType.startsWith("application/json")) {
      return await response.json();
    } else {
      return await response.blob();
    }
  }

  export async function fetch(
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

    const response = await window.fetch(
      Object.assign(getUrl(), { pathname }),
      request,
    );

    if (response.status === 200) {
      if (pathname === "/auth/password-login" && request.method === "POST") {
        const session: Session = await response.json();

        sessionStore.set(session);
      } else if (pathname === "/auth/logout" && request.method === "POST") {
        sessionStore.set(null);
      } else {
        return response;
      }
    } else if (response.status === 401) {
      sessionStore.set(null);
    }

    if (response.status >= 200 && response.status <= 300) {
      return response;
    } else {
      throw new ClientError(response);
    }
  }
</script>

<script lang="ts">
  import { page } from "$app/stores";

  const wrapFetch: typeof fetch = async function wrapFetch(
    pathname: string,
    method?: string,
    data?: any,
    headers?: Record<string, string>,
  ): Promise<Response> {
    const response = await fetch(pathname, method, data, headers);

    if (response.status === 401) {
      sessionStore.set(null);

      window.open($page.url, "_self");
    }

    return response;
  };
</script>

<slot fetch={wrapFetch} session={$sessionStore} />
