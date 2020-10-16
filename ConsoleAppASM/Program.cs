﻿using System.Diagnostics;

namespace ConsoleAppASM
{
    class Program
    {
        static void Main(string[] args)
        {
            A a = DynamicProxy.GetInstance<A>(new object[]{"2.0.0"});

            Trace.WriteLine("AM1's result is " + a.AM1(1, true));
            Trace.WriteLine("AM2's result is " + a.AM2(2, false));

        }
    }
}
