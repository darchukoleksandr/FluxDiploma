using NetworkCommsDotNet;

namespace Domain
{
    //TODO rename
    public class HostInfo
    {
        static HostInfo()
        {
            Ip = "127.0.0.1";
            Port = 42512;
            ConnectionInfo = new ConnectionInfo(Ip, Port);
        }
        public static ConnectionInfo ConnectionInfo { get; }
        public static string Ip { get; }
        public static int Port { get; }
    }

}
