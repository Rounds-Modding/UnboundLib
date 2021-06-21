using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnboundLib.Networking
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class UnboundRPC : Attribute
    {
        public string EventID { get; set; }
    }
}
