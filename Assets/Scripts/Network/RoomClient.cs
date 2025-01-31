using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Data;
using UnityEngine;

// For Debug.Log

namespace Network
{
    public class RoomClient   : NetworkRoom
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private bool _isConnected = false;

        public override async void Connect(RoomInfo roomInfo)
        {
            try  
            {
                _client = new TcpClient();
                _client.NoDelay = true;
                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                await _client.ConnectAsync(IPAddress.Parse(roomInfo.Ip), roomInfo.Port);
                _stream = _client.GetStream();
                _isConnected = true;
                Debug.Log($"Connected to server at {roomInfo.Ip}:{roomInfo.Port}");

                // Start listening for incoming messages  
                _ = Task.Run(() => ListenForMessages());
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection failed: {ex.Message}");
            }
        }

        private async Task ListenForMessages()
        {
            var buffer = new byte[1024];

            while (_isConnected)
            {
                try  
                {
                    int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Debug.Log($"Received message from server: {msg}");
                        await Awaitable.MainThreadAsync();
                        OnMessageReceived?.Invoke(msg);
                    }
                    else  
                    {
                        // Server disconnected  
                        Debug.LogWarning("Server disconnected.");
                        Disconnect();
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error while reading message: {ex.Message}");
                    Disconnect();
                }
            }
        }

        public override void SendChatMessage(string msg)
        {
            if (_isConnected)
            {
                byte[] data = Encoding.UTF8.GetBytes(msg);
                try  
                {
                    _stream.Write(data, 0, data.Length);
                    _stream.Flush();
                    Debug.Log($"Sent message to server: {msg}");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to send message: {ex.Message}");
                }
            }
            else  
            {
                Debug.LogWarning("Not connected to a server.");
            }
        }

        public void Disconnect()
        {
            _isConnected = false;
            _stream?.Close();
            _client?.Close();
            Debug.Log("Disconnected from server.");
        }
    }
}