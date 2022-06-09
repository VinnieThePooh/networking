namespace Sockets.Common.Infrastructure;

public static class DummyLogger
{
    public static void Log(string message) =>
        Console.WriteLine($"[Thread-{Thread.CurrentThread.ManagedThreadId}; DT:{DateTime.Now}]: {message}");
}