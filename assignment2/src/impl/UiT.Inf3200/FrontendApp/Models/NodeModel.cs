using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace UiT.Inf3200.FrontendApp.Models
{
    public class NodeModel
    {
        public Guid Guid { get; set; }

        public string Hostname { get; set; }

        public string DnsHostname => Dns.GetHostEntry(Hostname)?.HostName ?? Hostname;

        public int Port { get; set; }

        public NodeModel[] GetConnectedNodes()
        {
            var uriBuilder = new UriBuilder(Uri.UriSchemeHttp, Hostname, Port, "GetNodesWithGuids");
            WebRequest req = WebRequest.Create(uriBuilder.Uri) as HttpWebRequest;
            req.Method = WebRequestMethods.Http.Get;

            var connectedNodeList = new List<NodeModel>();
            using (var resp = req.GetResponse() as HttpWebResponse)
            {
                var respEncoding = string.IsNullOrWhiteSpace(resp.ContentEncoding) ? Encoding.ASCII : (Encoding.GetEncoding(resp.ContentEncoding) ?? Encoding.ASCII);
                using (var respReader = new StreamReader(resp.GetResponseStream()))
                {
                    Guid guid; string hostname; int port;
                    for (var respLine = respReader.ReadLine(); respLine != null; respLine = respReader.ReadLine())
                    {
                        if (string.IsNullOrWhiteSpace(respLine))
                            continue;
                        int spaceIdx = respLine.IndexOf(' ');
                        if (spaceIdx < 0)
                            continue;
                        guid = Guid.Parse(respLine.Substring(0, spaceIdx));
                        int colIdx = respLine.LastIndexOf(':');
                        hostname = respLine.Substring(spaceIdx + 1, colIdx - spaceIdx - 1);
                        port = int.Parse(respLine.Substring(colIdx + 1));
                        connectedNodeList.Add(new NodeModel { Guid = guid, Hostname = hostname, Port = port });
                    }
                }
            }

            return connectedNodeList.ToArray();
        }
    }
}
