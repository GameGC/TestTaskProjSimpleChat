namespace Data
{
    public class RoomInfo  
    {
        public string Name;
        public string Ip;
        public int Port;

        public RoomInfo(string name, string ip, int port)
        {
            Name = name;
            Ip = ip;
            Port = port;
        }
    }
}