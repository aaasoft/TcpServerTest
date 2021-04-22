using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServerTest.Core
{
    public class TcpServerContextOptions
    {
        public int Port { get; set; }
        public Action<long> ConnectCountHandler { get; set; }
        public Action<long> DisConnectCountHandler { get; set; }
        public Action<long> RecvCountHandler { get; set; }
        public Action<string> StatusHandler { get; set; }
        public int RecvTimeout { get; set; }
    }
}
