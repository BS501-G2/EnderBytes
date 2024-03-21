using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

public partial class Client
{
    // [JSExport]
    // internal static string Greeting()
    // {
    //     var text = $"Hello, World! Greetings from {GetHRef()}";
    //     Console.WriteLine(text);
    //     return text;
    // }

    // [JSImport("window.location.href", "main.js")]
    // internal static partial string GetHRef();

    [JSExport]
    internal static async Task<string> GetStatus()
    {
        await Task.Delay(1000);

        return "Aasds";
    }

    public static void Main()
    {
        Console.WriteLine("Hello world!");
    }
}
