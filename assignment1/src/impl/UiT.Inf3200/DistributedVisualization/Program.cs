using System;
using System.Windows.Forms;

namespace UiT.Inf3200.DistributedVisualization
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
