using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TcpMultiClientServer
{
    class Program
    {
        private static List<TcpClient> clients = new List<TcpClient>();
        private static readonly object clientLock = new object();

        static void Main(string[] args)
        {
            int port = 5000;
            TcpListener server = new TcpListener(IPAddress.Any, port);
            server.Start();
            Console.WriteLine($"Server started on port {port}");

            while (true)
            {
                TcpClient client = server.AcceptTcpClient();
                Console.WriteLine("Client connected");

                lock (clientLock)
                {
                    clients.Add(client);
                }

                Thread clientThread = new Thread(HandleClient);
                clientThread.Start(client);
            }
        }

        static void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;

            try
            {
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {message}");

                    // Prepare broadcast message
                    byte[] broadcastData = Encoding.UTF8.GetBytes(message);

                    // Broadcast to all clients
                    lock (clientLock)
                    {
                        foreach (TcpClient otherClient in clients.ToArray())
                        {
                            try
                            {
                                NetworkStream otherStream = otherClient.GetStream();
                                otherStream.Write(broadcastData, 0, broadcastData.Length);
                            }
                            catch
                            {
                                Console.WriteLine("Removing dead client.");
                                clients.Remove(otherClient);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Client connection error: " + ex.Message);
            }
            finally
            {
                lock (clientLock)
                {
                    clients.Remove(client);
                }
                stream.Close();
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
    }
}
