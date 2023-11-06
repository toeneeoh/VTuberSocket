/*
 * Tasks.cs
 *
 * Handles core VTuber plugin functionalities:
 * - Initializes and starts the VTuber plugin.
 * - Processes and forwards incoming requests.
 * - Manages request types
 */
    
using Newtonsoft.Json.Linq;
using VTS.Core;
using System.Threading.Tasks;
using System;

namespace VTuberSocket
{
    public class Tasks
    {
        private readonly ConsoleVTSLoggerImpl _logger = new();
        public CoreVTSPlugin Plugin;

        public Tasks(string pluginName, string authorName) =>
            Plugin = new(_logger, 100, pluginName, authorName, "");

        // Initialize and start the plugin
        public async Task StartPlugin()
        {
            try
            {
                var ws = new WebSocketNetCoreImpl(_logger);
                await Plugin.InitializeAsync(ws, new NewtonsoftJsonUtilityImpl(), 
                    new TokenStorageImpl(""), () => _logger.LogWarning("Disconnected!"));
                _logger.Log("Connected!");
            }
            catch (VTSException e) { _logger.LogError(e); }
        }

        // Continuously listen for and send requests
        public void WaitForInput()
        {
            while (true) { SendRequest(TCPServer.GetRequest()); Thread.Sleep(500); }
        }

        // Send request based on input type
        private async void SendRequest(JObject? input)
        {
            if (input == null) return;
            try
            {
                switch (input.Value<string>("messageType"))
                {
                    case "MoveModelRequest": 
                        HandleMoveModelRequest(input, "MoveModelRequest"); 
                        break;

                    case "HotkeyTriggerRequest": 
                        await ExecLog(() => Plugin.TriggerHotkeyAsync(input.Value<string>("hotkeyID")), 
                            "HotkeyTriggerRequest");
                        break;

                    case "CurrentModelRequest": 
                        Plugin.GetCurrentModel(a => TCPServer.SendResponse(Plugin.JsonUtility.ToJson(a)), 
                            b => _logger.LogError(b.data.message)); 
                        break;

                    case "ModelLoadRequest": 
                        await ExecLog(() => Plugin.LoadModelAsync(input.Value<string>("modelID")), 
                            "ModelLoadRequest"); 
                        break;
                }
            }
            catch (Exception e) { _logger.LogError(e); }
        }

        // Handle MoveModelRequest specifics
        private async void HandleMoveModelRequest(JObject input, string messageType)
        {
            var data = input.Value<JToken>("data")?.ToObject<VTSMoveModelData.Data>();
            if (data != null)
            {
                data.timeInSeconds = Math.Clamp(data.timeInSeconds, 0, 2);
                data.rotation = Math.Clamp(data.rotation, -360, 360);
                data.positionX = Math.Clamp(data.positionX, -1000, 1000);
                data.positionY = Math.Clamp(data.positionY, -1000, 1000);
                data.size = Math.Clamp(data.size, -100, 100);
                await ExecLog(() => Plugin.MoveModelAsync(data), messageType);
            }
            else { _logger.LogWarning($"{messageType} is empty!"); }
        }

        // Execute task and log results
        private async Task ExecLog(Func<Task> action, string msgType)
        {
            try { await action(); _logger.Log($"{msgType} succeeded."); } 
            catch (Exception e) { _logger.LogError(e); }
        }
    }
}