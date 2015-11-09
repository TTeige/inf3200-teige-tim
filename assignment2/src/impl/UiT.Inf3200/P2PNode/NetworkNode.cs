using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UiT.Inf3200.P2PNode
{
    public class NetworkNode
    {
        public string Hostname { get; set; }

        public int Port { get; set; }

        public bool IsUselessConnection { get; set; }
    }
}
