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
            StackTrace _st = new StackTrace();
            StackFrame _stack = _st.GetFrame(2);

            Trace.WriteLine("[" + level + "][" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "] " 
                + _stack.GetMethod().DeclaringType.FullName  + ":" + _stack.GetMethod().Name + " " + _message);
        }
    }
}
