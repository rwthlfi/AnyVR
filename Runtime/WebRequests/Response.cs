using System;

namespace WebRequests
{
    public abstract class Response
    {
        [NonSerialized]
        public string Error;
        [NonSerialized]
        public bool Success;
    }
}
