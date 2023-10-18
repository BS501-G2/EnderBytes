export enum ClientBrowserType {
  WebApp, Electron
}

export enum ClientFormFactor {
  PC, Mobile
}

export interface ClientConfiguration {
  browserType: ClientBrowserType
  formFactor: ClientFormFactor
} 

export class Client {
  static #client?: Client

  public static require(): Client {
    if (this.#client == null) {
      throw new Error("Client has not loaded.")
    }

    return this.#client
  }

  public static async get (configuration: ClientConfiguration): Promise<Client> {
    if (this.#client != null)
    {
      return this.#client
    }

    const client = new Client(configuration)
    await client.connect()
    return (this.#client = client)
  }

  public constructor (configuration: ClientConfiguration) {
    this.#configuration = configuration
  }

  #configuration: ClientConfiguration
  public get configuration (): ClientConfiguration { return structuredClone(this.#configuration) }

  public async connect(): Promise<void> {
  }
}