import EventEmitter, { type EventInterface } from '@rizzzi/eventemitter'

export interface ClientEvent extends EventInterface {
  stateUpdate: [state: number]
}

export class Client {
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
  }

  readonly #events: EventEmitter<ClientEvent>

  get #emit() { return this.#events.bind().emit }
  get on() { return this.#events.bind().on }
  get off() { return this.#events.bind().off }
  get once() { return this.#events.bind().once }
}
