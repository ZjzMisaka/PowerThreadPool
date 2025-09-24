using System;
using PowerThreadPool.Works;

namespace PowerThreadPool.Exceptions
{
    public class WorkExceptionBase : Exception
    {
        public WorkExceptionBase() { }

        /// <summary>
        /// work id
        /// </summary>
        public WorkID ID { get; internal set; }
    }
}
