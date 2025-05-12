using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Client_Server
{
    internal class Program
    {
        static void Main()
        {
            int port = 5000;
            TcpListener server = new TcpListener(IPAddress.Any, port);

            server.Start();
            Console.WriteLine("Server started. Waiting for connection...");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Client connected!");

                // Handle client in a new thread
                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        

        }

        static void HandleClient(object clientObject)
        {
            TcpClient client = (TcpClient)clientObject;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string received = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    //WE should do decryption
                    Console.WriteLine($"Received from client: {received}");

                    // Echo back
                    //We should do encryption
                    string response = "Server received: " + received;
                    byte[] responseData = Encoding.ASCII.GetBytes(response);
                    stream.Write(responseData, 0, responseData.Length);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Client disconnected: " + e.Message);
            }
            finally
            {
                stream.Close();
                client.Close();
            }
        }
    }
}
