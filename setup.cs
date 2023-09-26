namespace VTuberSocket;

class Setup
{
    private const string pluginName = "VTuberSocket";
    private const string authorName = "Tony";

    private const string serverIp   = "127.0.0.1";
    private const int serverPort    = 8001;

    static async Task Main(string[] args)
    {
        Tasks Plugin = new(pluginName, authorName);

        await Plugin.StartPlugin();
        _ = Task.Run(() => Plugin.WaitForInput());

        TCPServer.StartServer(serverIp, serverPort);

    }
}
