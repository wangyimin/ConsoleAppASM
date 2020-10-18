using System.Diagnostics;

namespace ConsoleAppASM
{
    public class A : IA
    {
        public string Ver = "1.0.0";

        public A(){}
        public A(string Ver) => this.Ver = Ver;

        [LoggerAttribute(Log = true, Before = typeof(ImplBeforeAfter))]
        public virtual int AM1(int param1, bool param2)
        {
            Trace.WriteLine("Method AM1 was called with following parameters: ");
            Trace.WriteLine($"Ver({Ver})[{nameof(param1)}:{param1}, {nameof(param2)}:{param2}]");
            return 10/param1;
        }
        public virtual int AM2(int param1, bool param2)
        {
            Trace.WriteLine("Method AM2 was called with following parameters: ");
            Trace.WriteLine($"Ver({Ver})[{nameof(param1)}:{param1}, {nameof(param2)}:{param2}]");
            return param1;
        }
    }

    public interface IA
    {
        int AM1(int param1, bool param2);
    }
}
