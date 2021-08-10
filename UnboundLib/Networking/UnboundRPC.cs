using System;

namespace UnboundLib.Networking
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UnboundRPC : Attribute
    {
        public string EventID { get; set; }
    }
}
