import EventEmitter, { type EventInterface } from '@rizzzi/eventemitter'

export class HybridWebSocket {
  public constructor(webSocket: WebSocket) {
    this.#webSocket = webSocket
  }

  readonly #webSocket: WebSocket
}

export interface ClientEvent extends EventInterface {
  stateUpdate: [state: number]
}

export class Client {
  static #initDotnet(url: URL, onStatusUpdate: (status: number) => void): any {
    return (<any>window).__DOTNET__INIT__(url, onStatusUpdate)
  }

  static #initializing: boolean = false
  static #client: Client | null = null

  public static getInstance(url: URL): Client {
    return Client.#client ??= (() => {
      try
      {
        Client.#initializing = true
        return new Client(url)
      }
      finally
      {
        Client.#initializing = false
      }
    })()
  }

  public constructor(url: URL) {
    if (!Client.#initializing) {
      throw new Error('Private constructor.')
    }

    this.#events = new EventEmitter({
      requireErrorHandling: false
    })

    this.#dotnet = Client.#initDotnet(url, (state) => this.#emit('stateUpdate', state))

    this.#events.on('stateUpdate', (state) => {
      console.log(state)
    })
  }

  readonly #events: EventEmitter<ClientEvent>
  readonly #dotnet: any

  #getInnerClient() { return this.#dotnet.RizzziGit.EnderBytes.Client.Library.Client }

  get #emit() { return this.#events.bind().emit }
  get on() { return this.#events.bind().on }
  get off() { return this.#events.bind().off }
  get once() { return this.#events.bind().once }

  get state(): number {
    return this.#getInnerClient().JsGetState()
  }

  #onConnectionChanged(isConnected: boolean) {
    return this.#getInnerClient().OnConnectionChanged(isConnected)
  }
}
