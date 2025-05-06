using System;

namespace PowerThreadPool.Exceptions
{
    public class WorkExceptionBase : Exception
    {
        public WorkExceptionBase() { }

        /// <summary>
        /// work id
        /// </summary>
        public string ID { get; internal set; }
    }
}
