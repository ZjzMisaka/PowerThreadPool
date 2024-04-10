using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerThreadPool.Results
{
    public enum Status { Succeed, Failed }

    public abstract class ExecuteResultBase
    {
        private string id;
        /// <summary>
        /// Work id.
        /// </summary>
        public string ID { get => id; set => id = value; }

        private Status status;
        /// <summary>
        /// Succeed or failed.
        /// </summary>
        public Status Status { get => status; internal set => status = value; }

        private Exception exception;
        /// <summary>
        /// If failed, Exception will be setted here.
        /// </summary>
        public Exception Exception { get => exception; internal set => exception = value; }
        internal abstract void SetExecuteResult(object result, Exception exception, Status status);
        internal abstract object GetResult();
        internal abstract ExecuteResult<object> ToObjResult();
    }
}
