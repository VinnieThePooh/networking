using ProcessInfo.Server.Enums;

namespace ProcessInfo.Server.Settings
{
    public class ServerSettings
    {
        public string ServerAddress { get; set; }

        public int ServerPort { get; set; }

        public NotificationMode NotificationMode { get; set; } = NotificationMode.Single;
    }
}
