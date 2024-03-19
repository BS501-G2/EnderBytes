export class Client {
  #initializing: boolean = false

  public static async newInstance(url: URL): Promise<Client> {
    const webSocket = new WebSocket("http://localhost:8080")

    webSocket.onmessage = (eve) => {
      console.log(eve.data)
    }

    const client = new Client()

    return client
  }

  public constructor() {
    if (!this.#initializing) {
      throw new Error('Private constructor.')
    }
  }
}
