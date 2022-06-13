using System;
using System.Collections.Generic;
using ProcessInfo.Server.Enums;

namespace ProcessInfo.Server
{
    public class ProcessInfoReceivedEventArgs : EventArgs
    {
        public ProcessInfoReceivedEventArgs(string processInfo)
        {
            ProcessInfo = processInfo;
            NotificationMode = NotificationMode.Single;
        }

        public ProcessInfoReceivedEventArgs(IEnumerable<string> processInfos)
        {
            ProcessInfos = processInfos;
            NotificationMode = NotificationMode.Batch;
        }


        public string ProcessInfo { get; }

        public IEnumerable<string> ProcessInfos { get; set; }

        public NotificationMode NotificationMode { get; }
    }
}