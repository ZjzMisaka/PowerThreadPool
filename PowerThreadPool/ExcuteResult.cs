using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool
{
    public enum Status { Succeed, Failed }
    public class ExcuteResult<TResult>
    {
        private TResult result;
        public TResult Result { get => result; set => result = value; }
        
        private Status status;
        public Status Status { get => status; set => status = value; }
        
        private Exception exception;
        public Exception Exception { get => exception; set => exception = value; }

        public ExcuteResult()
        { 
        
        }
    }
}
