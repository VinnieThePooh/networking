using ProcessInfo.Server.Enums;
using System;

namespace ProcessInfo.Server
{
    public class ProcessInfoReceivedEventArgs: EventArgs
    {
        public ProcessInfoReceivedEventArgs(string processInfo)
        {
            ProcessInfo = processInfo;
        }        

        public string ProcessInfo { get; }


    }
}
