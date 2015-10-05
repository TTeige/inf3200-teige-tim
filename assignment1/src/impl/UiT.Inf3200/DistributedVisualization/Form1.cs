using System;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace UiT.Inf3200.DistributedVisualization
{
    public partial class Form1 : Form
    {
        private Uri frontendUri;
        public Form1(string frontendAddress)
        {
            InitializeComponent();
            frontendUri = new Uri(frontendAddress);
        }

        private async void Form1_OnLoad(object sender, EventArgs e)
        {
            while (true)
            {
                var ringReq = WebRequest.Create(frontendUri);
                ringReq.Method = "DIAG";
                RingNode[] ringNodeArray;
                using (var ringResp = await ringReq.GetResponseAsync())
                {
                    var ringNodeSerializer = new XmlSerializer(typeof(RingNode[]), new XmlRootAttribute { ElementName = "Ring" });
                    ringNodeArray = (RingNode[])ringNodeSerializer.Deserialize(ringResp.GetResponseStream());
                }
                var allData = new System.Collections.Concurrent.ConcurrentBag<Tuple<int, string, byte[]>>();
                Parallel.ForEach(ringNodeArray, node =>
                {
                    var req = WebRequest.Create(node.NodeUri);
                    req.Method = "DIAG";

                    using (var resp = req.GetResponse())
                    {
                        var kvpsSerializer = new XmlSerializer(typeof(SerializableKeyValuePair));
                        var tmpKvps = kvpsSerializer.Deserialize(resp.GetResponseStream()) as SerializableKeyValuePair[];
                        foreach (var kvp in tmpKvps)
                        {
                            allData.Add(new Tuple<int, string, byte[]>(node.RingId, kvp.Key, kvp.Value));
                        }
                    }
                });

                this.listView1.Items.Clear();
                this.listView1.Items.AddRange(allData.Select(t => 
                    new ListViewItem(new string[] {t.Item1.ToString(), t.Item2, Encoding.Default.GetString(t.Item3)})).ToArray());
            }
        }
    }
}
