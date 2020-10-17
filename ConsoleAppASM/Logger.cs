using System;
using System.Diagnostics;

namespace ConsoleAppASM
{
    public sealed class Logger
    {
        public static void Debug(string message)
        {
            trace("DEBUG", message);
        }

        private static void trace(string level, string _message)
        {
            Trace.WriteLine($"[{level}][{DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff")}] {_message}");
        }
    }
}
