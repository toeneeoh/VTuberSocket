string serverIp = "127.0.0.1";
int serverPort = 8001;
string pluginName = "VTuberSocket";
string authorName = "Tony";

Console.Title = "VTubeSocket.exe";

Tasks PluginTasks = new(pluginName, authorName);

await PluginTasks.StartPlugin();
//await PluginTasks.StartConnection(serverIp, serverPort);

// background task
_ = Task.Run(() => PluginTasks.WaitForMessage());

PluginTasks.WaitForInput();

