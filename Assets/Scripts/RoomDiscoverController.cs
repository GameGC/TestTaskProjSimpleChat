using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Data;
using Network;
using UnityEngine;


public class RoomDiscoverController : MonoBehaviour
{
    public List<RoomInfo> Rooms => _client!=null ? _client.Rooms : _server.Rooms;
    public event Action<RoomInfo> OnRoomAdded;
    
    private MatchmakingClient _client;
    private MatchmakingServer _server;
    
    private void Awake()
    {
        _client = new MatchmakingClient();
        if (_client.IsServerAvailable())
        {
            _client.OnRoomAdded += OnRoomAddedFwd;
            Task.Run(_client.ConnectToServerAsync);
        }
        else
        {
            _client = null;
            _server = new MatchmakingServer();
            _server.OnRoomAdded += OnRoomAddedFwd;
            Task.Run(_server.StartServerAsync);
        }
    }

    private void OnDestroy()
    {
        if(_client != null)
            _client.OnRoomAdded -= OnRoomAddedFwd;
        else
        {
            _server.OnRoomAdded -= OnRoomAddedFwd;
            _server.StopServer();
        }
    }

    private void OnRoomAddedFwd(RoomInfo obj) => OnRoomAdded?.Invoke(obj);

    public void RegisterRoom(string name, string ip, int port)
    {
        if (_client != null)
            _client.RequestNewRoom(name, ip, port);
        else
            _server.RegisterRoom(name, ip, port);
    }
}