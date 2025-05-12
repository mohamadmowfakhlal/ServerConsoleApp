// ============================
// SERVER SIDE (Hybrid Encryption)
// ============================

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

class SecureServer
{
    static List<TcpClient> clients = new List<TcpClient>();
    static object clientLock = new object();
    static RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(2048);
    static Dictionary<TcpClient, byte[]> aesKeys = new Dictionary<TcpClient, byte[]>();

    static void Main()
    {
        int port = 5000;
        TcpListener server = new TcpListener(IPAddress.Any, port);
        server.Start();
        Console.WriteLine("Server started.");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Client connected.");
            lock (clientLock) clients.Add(client);
            new Thread(HandleClient).Start(client);
        }
    }

    static void HandleClient(object clientObj)
    {
        TcpClient client = (TcpClient)clientObj;
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[2048];

        try
        {
            // Step 1: Send RSA public key to client
            string publicKey = rsa.ToXmlString(false);
            byte[] publicKeyBytes = Encoding.UTF8.GetBytes(publicKey);
            stream.Write(publicKeyBytes, 0, publicKeyBytes.Length);

            // Step 2: Receive encrypted AES key from client
            int aesKeyLength = stream.Read(buffer, 0, buffer.Length);
            byte[] encryptedAesKey = new byte[aesKeyLength];
            Array.Copy(buffer, encryptedAesKey, aesKeyLength);
            byte[] aesKey = rsa.Decrypt(encryptedAesKey, false);
            aesKeys[client] = aesKey;

            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0) break;

                byte[] encryptedMsg = new byte[bytesRead];
                Array.Copy(buffer, encryptedMsg, bytesRead);

                using Aes aes = Aes.Create();
                aes.Key = aesKey;
                aes.IV = new byte[16];
                ICryptoTransform decryptor = aes.CreateDecryptor();
                string message = Encoding.UTF8.GetString(decryptor.TransformFinalBlock(encryptedMsg, 0, encryptedMsg.Length));

                Console.WriteLine("Received: " + message);

                // Broadcast encrypted message to all clients
                byte[] plainData = Encoding.UTF8.GetBytes(message);
                foreach (TcpClient otherClient in clients.ToArray())
                {
                    try
                    {
                        Aes broadcastAes = Aes.Create();
                        broadcastAes.Key = aesKeys[otherClient];
                        broadcastAes.IV = new byte[16];
                        ICryptoTransform encryptor = broadcastAes.CreateEncryptor();
                        byte[] encryptedData = encryptor.TransformFinalBlock(plainData, 0, plainData.Length);
                        otherClient.GetStream().Write(encryptedData, 0, encryptedData.Length);
                    }
                    catch
                    {
                        clients.Remove(otherClient);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Client error: " + ex.Message);
        }
        finally
        {
            stream.Close();
            client.Close();
            lock (clientLock)
            {
                clients.Remove(client);
                aesKeys.Remove(client);
            }
        }
    }
}