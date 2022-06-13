namespace ProcessInfo.Client.Settings
{
    public class NetworkingSettings
    {
        public string ServerAddress { get; set; }

        public int ServerPort { get; set; }

        //in seconds
        public int SendInterval { get; set; } = 10;
    }
}
