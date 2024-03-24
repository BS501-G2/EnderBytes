namespace RizzziGit.EnderBytes.Client.Library;

public enum ClientState : byte { Closed, Opening, Open, Closing, Borked }

public partial class Client
{
  private static ClientState StateBackingField = ClientState.Closed;

  public static ClientState State
  {
    get => StateBackingField;
    private set
    {
      StateBackingField = value;
      JsSetstate((int)StateBackingField);
    }
  }
}
