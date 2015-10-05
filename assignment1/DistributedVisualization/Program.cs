using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using UiT.Inf3200;
using UiT.Inf3200.StorageNodeServer;

namespace DistributedVisualization
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args != null && args.Length > 0)
                Application.Run(new Form1(args[0]));
        }
    }
}
