using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Data;
using UnityEngine;

namespace Network
{
    public class MatchmakingServer
    {
        private const int Port = 12345;
        private const string ServerIp = "127.0.0.1";
    
        public event Action<RoomInfo> OnRoomAdded;
    
        private TcpListener _server;
        public readonly List<RoomInfo> Rooms =  new List<RoomInfo>();
        private readonly List<TcpClient> _connectedClients = new List<TcpClient>(); // List of connected clients
    
        public async Task StartServerAsync()
        {
            _server = new TcpListener(IPAddress.Parse(ServerIp), Port);
            _server.Server.NoDelay = true;
            _server.Start();
            Debug.Log("Server started. Waiting for connections...");
            await ListenForClientsAsync();
        }
    
        public async void RegisterRoom(string name, string ip, int port)
        {
            Rooms.Add(new RoomInfo(name, ip, port));
            Debug.Log($"Room registered: {name} at {ip}:{port}");

            // Notify existing clients about the new room  
            NotifyClientsAboutNewRoom(name, ip, port);

            await Awaitable.MainThreadAsync();
            Debug.Log("room added");
            // Invoke the room added event  
            OnRoomAdded?.Invoke(Rooms[^1]);
            await Awaitable.BackgroundThreadAsync();
        }
    
        private string GetRoomList()
        {
            var roomList = new StringBuilder();
            foreach (var room in Rooms)
            {
                roomList.AppendLine($"{room.Name} - {room.Ip}:{room.Port}");
            }

            return roomList.ToString();
        }
        private async Task ListenForClientsAsync()
        {
            while (true)
            {
                var client = await _server.AcceptTcpClientAsync();
                Debug.Log("Client connected.");
                _connectedClients.Add(client); // Add the new client to the list  
                _ = HandleClientAsync(client); // Fire and forget 
                await Task.Yield();
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            var stream = client.GetStream();
            while(client.Connected)
            {
                if (!stream.DataAvailable)
                {
                    await Task.Yield();
                    continue;
                }

                var br = new BinaryReader(stream);
                var cmdId = (ServerCommandType)br.ReadByte();
                switch (cmdId)
                {
                    case ServerCommandType.GetRooms:
                    {
                        var roomList = GetRoomList();
                        byte[] response = Encoding.ASCII.GetBytes(roomList);
                        await stream.WriteAsync(response, 0, response.Length);
                        break;
                    }
                    case ServerCommandType.RequestAddRoom:
                    {
                        var name = br.ReadString();
                        var ip = br.ReadString();
                        var port = br.ReadInt32();

                        
                        RegisterRoom(name, ip, port);
                        break;
                    }
                }

                await Task.Yield();
            }

            Debug.Log("Client Disconnected");
            _connectedClients.Remove(client);
        }
    
        private async void NotifyClientsAboutNewRoom(string name, string ip, int port)
        {
            using var memoryStream = new MemoryStream();
            using var writer = new BinaryWriter(memoryStream);
        
            writer.Write((byte) 1); //  NEW_ROOM command
            writer.Write(name);
            writer.Write(ip);
            writer.Write(port);

            // Get the byte array from the memory stream  
            byte[] request = memoryStream.ToArray();

            foreach (var client in _connectedClients)
            {
                if (client.Connected) // Check if the client is still connected  
                {
                    Debug.Log(client.Connected);
                    var stream = client.GetStream();
                    try  
                    {
                        await stream.WriteAsync(request, 0, request.Length);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to send message to a client: {ex.Message}");
                    }
                }
            }
        }

        public void StopServer() => _server.Stop();
    }
}