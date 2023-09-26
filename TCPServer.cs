using System.Net.Sockets;
using System.Net;
using System.Text;

namespace VTuberSocket;
public static class TCPServer
{
    private const int BufferSize = 1024;
    private static int Port = 8001;
    private static IPAddress? localAddr;
    private static TcpListener? listener;
    private static string? incomingMessage;

    public static string? GetMessage()
    {
        string? output = incomingMessage;

        incomingMessage = null;

        return output;
    }

    async public static void RunServerAsync(string ip, int port)
    {
        Port = port;
        localAddr = IPAddress.Parse(ip);
        listener = new TcpListener(localAddr, Port);
        listener.Start();
        Console.WriteLine($"Server started on {localAddr}:{Port}");

        try
        {
            while(true)
            {
                await Accept(await listener.AcceptTcpClientAsync());
            }
        }
        finally { listener.Stop(); }
    }

    async static Task Accept(TcpClient client)
    {
        await Task.Yield();
        try
        {
            using (client)
            using (NetworkStream n = client.GetStream())
            {
                byte[] data = new byte[BufferSize];
                int bytesRead = 0;
                int chunkSize = 1;

                while (bytesRead < data.Length && chunkSize > 0)
                    bytesRead += chunkSize =
                        await n.ReadAsync(data, bytesRead, data.Length - bytesRead);

                // get string data
                incomingMessage = Encoding.Default.GetString(data);
                Console.WriteLine("[server] received : {0}", incomingMessage);

                // send the result to client
                string send_str = "server_send_test";
                byte[] send_data = Encoding.ASCII.GetBytes(send_str);
                await n.WriteAsync(send_data, 0, send_data.Length);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
