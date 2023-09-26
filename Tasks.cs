using VTS.Core;
using System.Net.Sockets;
using System.Text;

namespace VTuberSocket;
public class Tasks
{
    private const int UPDATE_INTERVAL_MS = 100;
    private const string ICON_NAME = "";
    private readonly ConsoleVTSLoggerImpl logger = new();

    public CoreVTSPlugin plugin;
    public TcpClient client = new();

    public Tasks(string pluginName, string authorName)
    {
        this.plugin = new(logger, UPDATE_INTERVAL_MS, pluginName, authorName, ICON_NAME);
    }

    /*
     * Establish plugin connection to VTube Studio
     */
    async public Task StartPlugin()
    {
        var websocket = new WebSocketNetCoreImpl(logger);
        var jsonUtility = new NewtonsoftJsonUtilityImpl();
        var tokenStorage = new TokenStorageImpl("");

        try
        {
            await plugin.InitializeAsync(websocket, jsonUtility, tokenStorage,
                () => logger.LogWarning("Disconnected!"));
        }
        catch (VTSException e)
        {
            logger.LogError(e);
        }

        logger.Log("Connected!");
    }

    /*
     * Executes string inputs from console or server messages
     */
    public void WaitForInput()
    {
        string? input;

        while (true) {
            input = TCPServer.GetMessage() ?? Console.ReadLine();

            if(input is not null)
                ExecuteCommand(input);
        }
    }

    /*
     * Translates string inputs into VTube Studio API requests
     */
    public async void ExecuteCommand(string input)
    {
        string[] arguments = input.Split(" ");

        switch (arguments[0])
        {
            case "connect":
                await this.StartPlugin();
                break;
            case "disconnect":
                this.plugin.Disconnect();
                break;
            case "rotate":
                float rotation = 180f;
                float seconds = 1f;
                bool relative = true;

                try
                {
                    rotation = float.Parse(arguments[1]);
                    seconds = float.Parse(arguments[2]);
                    relative = bool.Parse(arguments[3]);

                    if (rotation > 360)
                        rotation = 360;
                    else if (rotation < -360)
                        rotation = -360;

                    if (seconds > 2)
                        seconds = 2;
                    else if (seconds < 0)
                        seconds = 0;
                }
                catch (Exception e) { }
                finally
                {
                    if (this.plugin is not null)
                        await RotateModelAsync(this.plugin, rotation, seconds, relative);
                }
                break;
        }
    }

    /*
     * API Requests
     */
    private static async Task RotateModelAsync(CoreVTSPlugin plugin, float rotate, float seconds, bool relative)
    {
        VTSMoveModelData.Data request = new()
        {
            rotation = rotate,
            timeInSeconds = seconds,
            valuesAreRelativeToModel = relative
        };

        await plugin.MoveModelAsync(request);
    }

}
