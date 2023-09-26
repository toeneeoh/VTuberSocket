using System.Runtime.InteropServices;
using System.Diagnostics;
using VTS.Core;
using System.Net.Sockets;
using System.Text;
using VTuberSocket.Implementations;

public class Tasks
{
    public CoreVTSPlugin plugin;
    private readonly ConsoleVTSLoggerImpl logger = new();
    public TcpClient client = new();
    public bool pluginIsRunning = false;

    public Tasks(string pluginName, string authorName)
    {
        this.plugin = new(logger, 100, pluginName, authorName, "");
    }

    /*
     * Establish connection to VTube Studio
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
        this.pluginIsRunning = true;
    }

    /*
     * Establish connection with TCP Client
     */
    public async Task StartConnection(string serverIp, int serverPort)
    {
        try
        {
            await client.ConnectAsync(serverIp, serverPort);
            Console.WriteLine($"Connected to {serverIp}:{serverPort}");
        }
        catch (Exception)
        {
            Console.WriteLine("Error: failed to establish connection.");
        }

        Thread.Sleep(1000);
    }

    /*
     * Receives input from console
     */
    public void WaitForInput()
    {
        string? input;

        while (true) {
            input = Console.ReadLine()!;

            if(input != null)
            {
                ExecuteCommand(input);
            }

            Thread.Sleep(500);
        }
    }

    /*
     * Receives messages from TCP Client
     */
    async public void WaitForMessage()
    {
        while (client is not null)
        {
            try
            {
                byte[] receiveBuffer = new byte[1024];
                int bytesRead = await client.GetStream().ReadAsync(receiveBuffer, 0, receiveBuffer.Length);

                if (bytesRead > 0)
                {
                    string receivedData = Encoding.ASCII.GetString(receiveBuffer, 0, bytesRead);
                    ExecuteCommand(receivedData);
                }
                else
                {
                    Console.WriteLine("No data received.");
                }

            }
            catch (Exception) { Console.WriteLine("Something went wrong..."); }

            Thread.Sleep(500);
        }
    }

    /*
     * Handling for command line inputs and messages from TCP Client
     */
    async void ExecuteCommand(string input)
    {
        string[] arguments = input.Split(" ");

        switch (arguments[0])
        {
            case "connect":
                await this.StartPlugin();
                break;
            case "connectTCP":
                try
                {
                    await StartConnection(arguments[1], int.Parse(arguments[2]));
                }
                catch (Exception) { Console.WriteLine("Invalid arguments, requires IP address and port."); }

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
        VTSMoveModelData.Data request = new();
        
        request.rotation = rotate;
        request.timeInSeconds = seconds;
        request.valuesAreRelativeToModel = relative;

        await plugin.MoveModelAsync(request);
    }

}
