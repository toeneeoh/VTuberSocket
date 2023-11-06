/*
 * Setup.cs
 *
 * Initializes and runs the VTuberSocket plugin and server.
 * Configured with default IP, port, and author details.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VTuberSocket
{
    class Setup
    {
        static async Task Main(string[] args)
        {
            var path = "src/py/config.py";
            var configData = ReadConfig(path, new HashSet<string> 
                { "C_SHARP_SERVER_IP", "C_SHARP_SERVER_PORT", "C_SHARP_AUTHOR_NAME" });

            if (!configData.TryGetValue("C_SHARP_SERVER_IP", out string? serverIp)) 
                throw new Exception("IP missing in config");
            serverIp = serverIp.Trim('"');

            if (!configData.TryGetValue("C_SHARP_SERVER_PORT", out string? portStr) ||
                !int.TryParse(portStr.Trim('"'), out int serverPort)) 
                throw new Exception("Invalid port in config");
                
            if (!configData.TryGetValue("C_SHARP_AUTHOR_NAME", out string? authorName)) 
                throw new Exception("Author name missing in config");
            authorName = authorName.Trim('"');

            var plugin = new Tasks("VTuberSocket", authorName);
            await plugin.StartPlugin();
            _ = Task.Run(() => plugin.WaitForInput());
            TCPServer.StartServer(serverIp, serverPort);
        }

        static Dictionary<string, string> ReadConfig(string path, HashSet<string> keys)
        {
            var data = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(path))
            {
                if (!line.TrimStart().StartsWith("#") && line.Contains('='))
                {
                    var parts = line.Split('=');
                    if (keys.Contains(parts[0].Trim())) data[parts[0].Trim()] = parts[1].Trim();
                }
            }
            return data;
        }
    }
}