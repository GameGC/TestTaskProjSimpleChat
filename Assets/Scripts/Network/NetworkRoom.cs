using System;
using Data;

namespace Network
{
    public abstract class NetworkRoom
    {
        public Action<string> OnMessageReceived;

        public abstract void Connect(RoomInfo roomInfo);

        public abstract void SendChatMessage(string message);
    }
}