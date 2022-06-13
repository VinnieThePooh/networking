using ProcessInfo.Server.Enums;
using System;

namespace ProcessInfo.Server
{
    public class ProcessInfoReceivedEventArgs: EventArgs
    {
        public ProcessInfoReceivedEventArgs(string processInfo)
        {
            ProcessInfo = processInfo;
            NotificationMode = NotificationMode.Single;
        }

        public ProcessInfoReceivedEventArgs(string[] processInfos)
        {
            ProcessInfos = processInfos;
            NotificationMode = NotificationMode.Batch;
        }
        

        public string ProcessInfo { get; }

        public string[] ProcessInfos { get; set; }

        public NotificationMode NotificationMode { get; }

    }
}
