import { EventEmitter, type EventInterface } from '@rizzzi/eventemitter'

export interface ClientEvent extends EventInterface {
  ready: []
  error: [error: Error]

  sessionTokenChange: [sessionToken: string | null]
}

export class ClientError extends Error {
  public constructor(response: Response) {
    super(`HTTP ${response.status}: ${response.statusText}`)

    this.response = response
  }

  readonly response: Response

  public get error(): Response { return this.response }
}

export class Client {
  static #initializing: boolean = false
  static #client: Client | null = null

  public static getInstance(url: URL): Client {
    return Client.#client ??= (() => {
      try {
        Client.#initializing = true
        return new Client(url)
      }
      finally {
        Client.#initializing = false
      }
    })()
  }

  public constructor(url: URL) {
    if (!Client.#initializing) {
      throw new Error('Private constructor.')
    }

    this.#url = url
    this.#sessionToken = null
    this.#events = new EventEmitter({
      requireErrorHandling: false
    })

    void this.#emit('ready')
  }

  readonly #url: URL
  readonly #sessionToken: string | null
  readonly #events: EventEmitter<ClientEvent>

  get #emit() { return this.#events.bind().emit }
  get on() { return this.#events.bind().on }
  get off() { return this.#events.bind().off }
  get once() { return this.#events.bind().once }

  get sessionToken(): string | null {
    return this.#sessionToken
  }

  #setSessionToken(sessionToken: string | null) {
    this.#emit('sessionTokenChange', sessionToken)
  }

  async setSessionToken(sessionToken: string | null): Promise<void> {
    if (sessionToken != null) {
      const response = await this.fetch('/auth/verify', 'get', {
        headers: {
          Authorization: `EDCustom ${sessionToken}`,
        }
      })

      this.#setSessionToken(response && (response.status >= 200 && response.status < 300) ? sessionToken : null)
    } else {
      this.#setSessionToken(null)
    }
  }

  public async fetch(path: string, method: string, options: RequestInit): Promise<Response | null> {
    const url = new URL(this.#url.href)
    url.pathname = path
    try {
      const response = await fetch(url, Object.assign(structuredClone(options), { method }))

      if (response.status == 401) {
        this.#setSessionToken(null)
      }

      if (!(response.status >= 200 && response.status < 300)) {
        void this.#emit('error', new ClientError(response))
      }

      return response
    } catch (error: any) {
      void this.#emit('error', error)

      return null
    }
  }

  public async putFile()
  {

  }
}
