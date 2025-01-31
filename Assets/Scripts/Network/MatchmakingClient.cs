using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Data;
using UnityEngine;
using static Data.ServerCommandType;

namespace Network
{
    public class MatchmakingClient
    {
        private const int Port = 12345;
        private const string ServerIp = "127.0.0.1";
    
        public readonly List<RoomInfo> Rooms = new List<RoomInfo>();

        public event Action<RoomInfo> OnRoomAdded;

        private TcpClient _client;
        private NetworkStream _clientSteam;

        private Thread _bgThread;
    
        public bool IsServerAvailable()
        {
            _client = null;
            try
            {
                _client = new TcpClient();
                _client.NoDelay = true;
                _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                _client.Connect(ServerIp, Port);
              
                return true;
            }
            catch
            {
                if (_client != null)
                {
                    _client.Close();
                    _client.Dispose();
                }
                return false;
            }
        }
    
        public async Task ConnectToServerAsync()
        {
            Debug.Log("ConnectToServerAsync");
            if (_client == null || !_client.Connected)
            {
                Debug.LogError("TcpClient is null or not connected.");
                return;
            }

            _clientSteam = _client.GetStream();
            await GetAllRoomsAsync();

            // Use Task.Run to handle notifications in the background  
            _ = Task.Run(ListenForNotificationsAsync);
        }
    
        private async Task GetAllRoomsAsync()
        {
            try
            {
                var bw = new BinaryWriter(_clientSteam);
                bw.Write((byte) GetRooms);

                // Read response  
                byte[] response = new byte[1024];
                int bytesRead = await _clientSteam.ReadAsync(response, 0, response.Length);
                string roomData = Encoding.ASCII.GetString(response, 0, bytesRead);

                await Awaitable.MainThreadAsync();
                UpdateRoomList(roomData);
            }
            catch (IOException ioEx)
            {
                Debug.LogError($"I/O error occurred: {ioEx.Message}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"An error occurred: {ex.Message}");
            }
        }
        private void UpdateRoomList(string data)
        {
            foreach (var line in data.Split(new[] {'\n'}, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = line.Split(new[] {'-'}, 2);
                if (parts.Length == 2)
                {
                    var roomInfo = parts[0].Trim();
                    var ipPort = parts[1].Trim().Split(':');
                    if (ipPort.Length == 2 && int.TryParse(ipPort[1], out var port))
                    {
                        Rooms.Add(new RoomInfo(roomInfo, ipPort[0].Trim(), port));
                        OnRoomAdded?.Invoke(Rooms[^1]);
                    }
                }
            }
        }
    
        private async void ListenForNotificationsAsync()
        {
            var reader = new BinaryReader(_client.GetStream());
            
            while (true)
            {
                try
                {
                    if(_clientSteam.DataAvailable  || _client.Available > 0)
                    {
                        Debug.Log("load rooms");
                        byte notificationType = reader.ReadByte();
                        string name = reader.ReadString();
                        string ip = reader.ReadString();
                        int port = reader.ReadInt32();

                        // Process the notification based on the type  
                        if (notificationType == 1) // Check for NEW_ROOM notification  
                        {
                            Rooms.Add(new RoomInfo(name, ip, port));
                            await Awaitable.MainThreadAsync();
                            OnRoomAdded?.Invoke(Rooms[^1]);
                            await Awaitable.BackgroundThreadAsync();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error while listening for notifications: {ex.Message}");
                    break; // Exit the loop on error  
                }

                await Task.Delay(100);
            }
        }
    
        public async void RequestNewRoom(string name, string ip, int port)
        {
            var memory = new MemoryStream();
            var bw = new BinaryWriter(memory);
            bw.Write((byte) RequestAddRoom);
            bw.Write(name);
            bw.Write(ip);
            bw.Write(port);


            await _clientSteam.WriteAsync(memory.ToArray());
            Debug.Log("RequestNewRoom");
        }

        public void Disconnect()
        {
            _bgThread.Abort();
            _clientSteam.Close();
        }
    }
}