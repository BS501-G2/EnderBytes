export class HybridWebSocket {
  public constructor(webSocket: WebSocket) {
    this.#webSocket = webSocket
  }

  readonly #webSocket: WebSocket
}

export class Client {
  static get #dotnet(): any { return (<any>window).__DOTNET__ }
  static #initializing: boolean = false
  static #client: Client | null = null
  static #clientPromise: Promise<Client> | null = null

  public static getInstance(url: URL): Promise<Client> {
    if (Client.#client != null) {
      return Promise.resolve(Client.#client)
    }
    else if (Client.#clientPromise != null) {
      return Client.#clientPromise
    }

    return this.#clientPromise = (new Promise((resolve, reject) => {
      const webSocket = new WebSocket(url)

      webSocket.onerror = reject
      webSocket.onopen = () => {
        this.#initializing = true
        const client = new Client(webSocket)
        this.#initializing = false

        resolve(this.#client = client)
        this.#clientPromise = null
      }
    }))
  }

  public constructor(webSocket: WebSocket) {
    if (!Client.#initializing) {
      throw new Error('Private constructor.')
    }

    this.#webSocket = webSocket

    this.#webSocket.readyState
  }

  readonly #webSocket: WebSocket

  get isOpen(): boolean { return this.#webSocket.readyState === 1 }

  async test(): Promise<string> { return await Client.#dotnet.Client.GetStatus() }
}
