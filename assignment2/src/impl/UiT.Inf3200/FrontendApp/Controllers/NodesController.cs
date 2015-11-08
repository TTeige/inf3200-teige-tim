using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.IO;
using System.Diagnostics;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace UiT.Inf3200.FrontendApp.Controllers
{
    public class NodesController : Controller
    {
        public static ConcurrentBag<Tuple<string, int>> Nodes { get; } = new ConcurrentBag<Tuple<string, int>>();

        // GET: /<controller>/
        public IActionResult Index()
        {
            return View(Nodes.ToArray());
        }

        public IActionResult Details(string hostname, int port)
        {
            var dnsHostEntry = Dns.GetHostEntry(hostname);
            var dnsHostname = dnsHostEntry != null ? dnsHostEntry.HostName : hostname;

            Tuple<string, int> nodeTuple = Nodes.ToArray().FirstOrDefault(t =>
            {
                var hostnameMatch = string.Equals(hostname, t.Item1, StringComparison.OrdinalIgnoreCase);
                if (!hostnameMatch)
                {
                    var tDnsHostEntry = Dns.GetHostEntry(t.Item1);
                    var tDnsHostname = tDnsHostEntry != null ? tDnsHostEntry.HostName : t.Item1;
                    hostnameMatch = string.Equals(tDnsHostname, dnsHostname, StringComparison.OrdinalIgnoreCase);
                }
                return hostnameMatch && t.Item2 == port;
            });

            if (nodeTuple == null)
                return HttpNotFound();
            var nodeUriBuilder = new UriBuilder(Uri.UriSchemeHttp, nodeTuple.Item1, nodeTuple.Item2);
            var req = WebRequest.Create(new Uri(nodeUriBuilder.Uri, "getNodes")) as HttpWebRequest;
            req.Method = WebRequestMethods.Http.Get;

            var nodeConnectionList = new List<Tuple<string, int>>();
            try
            {
                using (var resp = req.GetResponse() as HttpWebResponse)
                {
                    var respEncodingName = resp.ContentEncoding;
                    var respEncoding = string.IsNullOrWhiteSpace(respEncodingName) ? Encoding.ASCII : Encoding.GetEncoding(respEncodingName);
                    using (var respReader = new StreamReader(resp.GetResponseStream()))
                    {
                        for (string respLine = respReader.ReadLine(); respLine != null; respLine = respReader.ReadLine())
                        {
                            if (string.IsNullOrWhiteSpace(respLine))
                                continue;
                            string cHostname; int cPort;
                            int colIdx = respLine.LastIndexOf(':');
                            if (colIdx < 0)
                            {
                                cHostname = respLine;
                                cPort = 8899;
                            }
                            else
                            {
                                cHostname = respLine.Substring(0, colIdx);
                                cPort = int.Parse(respLine.Substring(colIdx + 1));
                            }
                            nodeConnectionList.Add(Tuple.Create(cHostname, cPort));
                        }
                    }
                }
            }
            catch (WebException webExcept)
            {
                ViewData["NodeConnectionError"] = $"Could not retrieve node connections! {webExcept}";
            }

            ViewBag.NodeConnections = nodeConnectionList;

            return View(nodeTuple);
        }
    }
}
