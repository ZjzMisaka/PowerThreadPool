using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool
{
    public enum Status { Succeed, Failed }
    public class ExecuteResult<TResult>
    {
        private string id;
        public string ID { get => id; set => id = value; }

        private TResult result;
        public TResult Result { get => result; internal set => result = value; }
        
        private Status status;
        public Status Status { get => status; internal set => status = value; }
        
        private Exception exception;
        public Exception Exception { get => exception; internal set => exception = value; }

        internal ExecuteResult()
        { 
        
        }
    }
}
