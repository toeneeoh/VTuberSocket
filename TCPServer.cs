using System.Net.Sockets;
using System.Net;
using System.Text;

namespace VTuberSocket;

public static class TCPServer
{
    private const int BufferSize = 1024;
    private static string? incomingMessage;

    public static string? GetMessage()
    {
        string? output = incomingMessage;

        incomingMessage = null;

        return output;
    }

    public static int StartServer(string ip, int port)
    {
        var localAddr = IPAddress.Parse(ip);
        TcpListener server = new TcpListener(localAddr, port);

        try
        {
            server.Start();
            Console.WriteLine($"Server started on {localAddr}:{port}");
            Byte[] bytes = new Byte[BufferSize];

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Connected!");

                NetworkStream stream = client.GetStream();
                int i;

                try
                {
                    while (client.Connected && (i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        string data = Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine($"Received: {data}");

                        incomingMessage = data;

                        if (data == "SHUTDOWN_SIGNAL")
                        {
                            Console.WriteLine("Shutdown signal received. Terminating server.");
                            return 0;  // exit the application
                        }

                        byte[] msg = Encoding.ASCII.GetBytes(data.ToUpper());

                        try
                        {
                            stream.Write(msg, 0, msg.Length);
                            Console.WriteLine($"Sent: {data.ToUpper()}");
                        }
                        catch (IOException ioEx)
                        {
                            Console.WriteLine($"IOException encountered while writing: {ioEx.Message}");
                            break;  // exit the loop if the client has disconnected
                        }
                    }
                }
                catch (SocketException ex) { Console.WriteLine($"Client disconnected: {ex.Message}"); }
                catch (ObjectDisposedException) { Console.WriteLine("NetworkStream has been disposed of."); }
                finally { client.Close(); }
            }
        }
        catch (Exception e) { Console.WriteLine($"Exception: {e}"); }
        finally { server.Stop(); }

        return 0;
    }
}
