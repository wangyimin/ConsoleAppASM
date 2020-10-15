using System;
using System.Diagnostics;

namespace ConsoleAppASM
{
    public sealed class Logger
    {
        private static string _WRAPPER_ = "Wrapper";

        public static void Debug(string message)
        {
            trace("DEBUG", message);
        }

        private static void trace(string level, string _message)
        {
            StackTrace _st = new StackTrace();
            StackFrame _stack = _st.GetFrame(2);

            string _cls = _stack.GetMethod().DeclaringType.FullName;
            _cls = _cls.IndexOf(_WRAPPER_) == 0 ? _cls.Substring(_WRAPPER_.Length) : _cls;

            Trace.WriteLine("[" + level + "][" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss.fff") + "] " 
                + _cls + ":" + _stack.GetMethod().Name + " " + _message);
        }
    }
}
