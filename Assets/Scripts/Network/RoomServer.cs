using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Data;
using UnityEngine;

namespace Network
{
    public class RoomServer: NetworkRoom
    {

        private TcpListener _server;
        private List<TcpClient> _clients = new List<TcpClient>();

        public override void Connect(RoomInfo roomInfo)
        {
            _server = new TcpListener(IPAddress.Parse(roomInfo.Ip), roomInfo.Port);
            _server.Server.NoDelay = true;
            _server.Start();
            Debug.Log($"Server started on {roomInfo.Ip}:{roomInfo.Port}");
            HandleClients();
        }

        private async void HandleClients()
        {
            while (true)
            {
                var client = await _server.AcceptTcpClientAsync();
                _clients.Add(client);
                Debug.Log($"Client connected");
                _ = Task.Run(() => HandleClient(client));
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            var buffer = new byte[1024];
            var stream = client.GetStream();

            while (true)
            {
                try  
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break; // Client disconnected  
                    string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Debug.Log($"Received message: {msg}");
                    SendChatMessage(msg);

                    await Awaitable.MainThreadAsync();
                    OnMessageReceived?.Invoke(msg);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error: {ex.Message}");
                    break;
                }
            }
        
            client.Close();
            _clients.Remove(client);
            Debug.Log($"Client disconnected: {((IPEndPoint)client.Client.RemoteEndPoint).Address}");
        }

        public override void SendChatMessage(string msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            foreach (var client in _clients)
            {
                try  
                {
                    var stream = client.GetStream();
                    stream.Write(data, 0, data.Length);
                    Debug.Log($"Sent message to client: {msg}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to send message to client: {ex.Message}");
                }
            }
        }
    }
}