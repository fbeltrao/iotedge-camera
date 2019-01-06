using System;

namespace CameraModule
{
    internal static class Logger
    {
        internal static void Log(string text)
        {
            Console.WriteLine($"[{DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff")}] {text}");
        }

        internal static void LogError(Exception ex, string text)
        {
            Log(string.Concat(text, System.Environment.NewLine, ex.ToString()));
        }

    }
}
