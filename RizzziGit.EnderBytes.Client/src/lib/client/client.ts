let initializing: boolean = false
export class Client {

  public static async newInstance(url: URL): Promise<Client> {
    const webSocket = new WebSocket(url)

    webSocket.onmessage = (event) => {
      console.log(event.data)
    }

    initializing = true
    const client = new this()
    initializing = false
    return client
  }

  public constructor() {
    if (!initializing) {
      throw new Error('Private constructor.')
    }
  }
}
