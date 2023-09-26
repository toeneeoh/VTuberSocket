using System.Runtime.InteropServices;
using System.Diagnostics;
using VTS.Core;
using System.Net.Sockets;
using System.Text;
using VTuberSocket.Implementations;

public class Tasks
{
    private readonly ConsoleVTSLoggerImpl logger;
    public CoreVTSPlugin plugin;
    public TcpClient? client = null;
    public bool pluginIsRunning = false;
    public bool isRunning = false;

    public Tasks(string pluginName, string authorName)
    {
        logger = new ConsoleVTSLoggerImpl();
        this.plugin = new(logger, 100, pluginName, authorName, "");
    }

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

    public void EstablishConnection(string serverIp, int serverPort)
    {
        while (true)
        {
            if (client is null)
            {
                try
                {
                    this.client = new(serverIp, serverPort);
                    Console.WriteLine($"Connected to {serverIp}:{serverPort}");
                }
                catch (Exception)
                {
                    Console.WriteLine("Error: failed to establish connection.");
                }
            }
            else if (!client.Connected)
            {
                client = null;
            }

            Thread.Sleep(2000);
        }
    }

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

    public void WaitForMessage()
    {
        while (client is not null)
        {
            // Get the network stream for sending and receiving data
            try
            {
                NetworkStream stream = client.GetStream();

                // Receive a message
                byte[] buffer = new byte[1024];
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                ExecuteCommand(response);
            }
            catch (Exception)
            {
                client = null;
            }

            Thread.Sleep(500);
        }
    }

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
                    await Task.Run(() => EstablishConnection(arguments[1], int.Parse(arguments[2])));
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

    static bool isProcessOpen(string processName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Windows
            Process[] processes = Process.GetProcessesByName(processName);
            return processes.Length > 0;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // macOS and Linux
            Process process = new();
            process.StartInfo.FileName = "pgrep";
            process.StartInfo.Arguments = $"-x {processName}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return !string.IsNullOrEmpty(output);
        }
        else
        {
            throw new PlatformNotSupportedException("Platform not supported.");
        }
    }

    // model behaviors
    private static async Task RotateModelAsync(CoreVTSPlugin plugin, float rotate, float seconds, bool relative)
    {
        VTSMoveModelData.Data request = new();
        
        request.rotation = rotate;
        request.timeInSeconds = seconds;
        request.valuesAreRelativeToModel = relative;

        await plugin.MoveModelAsync(request);
    }

}
