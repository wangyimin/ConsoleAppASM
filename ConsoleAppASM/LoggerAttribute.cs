﻿using System;

namespace ConsoleAppASM
{
    public class LoggerAttribute : Attribute
    {
        public bool Log { get; set; }
        public Type Before { get; set; }
    }
}
