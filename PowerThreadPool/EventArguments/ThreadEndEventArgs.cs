﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.EventArguments
{
    public class ThreadEndEventArgs : EventArgs
    {
        public ThreadEndEventArgs() { }

        private string id;
        public string ID { get => id; set => id = value; }

        private object result;
        public object Result { get => result; internal set => result = value; }

        private Status status;
        public Status Status { get => status; internal set => status = value; }

        private Exception exception;
        public Exception Exception { get => exception; internal set => exception = value; }
    }
}