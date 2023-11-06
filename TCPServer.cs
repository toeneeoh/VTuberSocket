/*
 * TCPServer.cs
 *
 * Manages the TCP server for:
 * - Receiving incoming JSON requests.
 * - Sending outgoing responses.
 * - Maintaining client communications.
 */

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json.Linq;

namespace VTuberSocket
{
    public static class TCPServer
    {
        private static readonly ConcurrentQueue<JObject> _reqQ = new();
        private static readonly ConcurrentQueue<string> _resQ = new();

        // Retrieve request or return null if empty
        public static JObject? GetRequest() => _reqQ.TryDequeue(out var r) ? r : null;

        // Add a response to the outgoing queue
        public static void SendResponse(string response) => _resQ.Enqueue(response);

        // Start the TCP server and listen for client connections
        public static int StartServer(string ip, int port)
        {
            var addr = IPAddress.Parse(ip);
            var server = new TcpListener(addr, port);
            try
            {
                server.Start();
                Console.WriteLine($"Server started on {addr}:{port}");
                while (true) HandleClient(server.AcceptTcpClient());  // Listen continuously
            }
            catch (Exception e) { Console.WriteLine($"Error: {e.Message}"); }
            finally { server.Stop(); }
            return 0;
        }

        // Process communications with a specific client
        private static void HandleClient(TcpClient client)
        {
            using var stream = client.GetStream();
            var buffer = new byte[1024];

            // Read data and if valid JSON, add to queue
            while (stream.Read(buffer, 0, 1024) is int bytesRead && bytesRead > 0)
            {
                var data = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                if (data == "SHUTDOWN_SIGNAL") Environment.Exit(0);
                if (TryParseJson(data, out var request) && request != null) _reqQ.Enqueue(request);
            }

            // Send the response if available
            if (_resQ.TryDequeue(out var response))
                stream.Write(Encoding.UTF8.GetBytes(response), 0, response.Length);
        }

        // Convert string data to JSON if possible
        private static bool TryParseJson(string data, out JObject? json)
        {
            try { json = JObject.Parse(data); return true; }
            catch { json = null; return false; }
        }
    }
}