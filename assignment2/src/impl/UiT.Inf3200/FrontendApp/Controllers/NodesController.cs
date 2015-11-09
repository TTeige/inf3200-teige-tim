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
using UiT.Inf3200.FrontendApp.Models;
using System.Net.Mime;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace UiT.Inf3200.FrontendApp.Controllers
{
    public class NodesController : Controller
    {
        private static readonly Random Randomizer = new Random();
        public static ConcurrentDictionary<Guid, Tuple<string, int>> Nodes { get; } = new ConcurrentDictionary<Guid, Tuple<string, int>>();

        // GET: /<controller>/
        public IActionResult Index()
        {
            var nodes = Nodes.ToArray().Select(kvp => new NodeModel { Guid = kvp.Key, Hostname = kvp.Value.Item1, Port = kvp.Value.Item2 }).ToArray();

            return View(nodes);
        }

        public IActionResult Details(Guid guid)
        {
            var model = GetModelForGuid(guid);

            return View(model);
        }

        public async Task<IActionResult> DisconnectAll()
        {
            var models = Nodes.ToArray().Select(kvp => new NodeModel { Guid = kvp.Key, Hostname = kvp.Value.Item1, Port = kvp.Value.Item2 }).ToArray();

            await Task.WhenAll(models.Select(m => SendDisconnectToModel(m)));

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> ReconnectAll()
        {
            var models = Nodes.ToArray().Select(kvp => new NodeModel { Guid = kvp.Key, Hostname = kvp.Value.Item1, Port = kvp.Value.Item2 }).ToArray();

            await Task.WhenAll(models.Select(m => SendDisconnectToModel(m)));
            await Task.WhenAll(models.Select(m => SendConnectToModel(m, GenerateRandomConnectedNodes(m))));

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Disconnect(Guid guid)
        {
            var model = GetModelForGuid(guid);

            await SendDisconnectToModel(model);

            return RedirectToAction(nameof(Details), new { guid = guid });
        }

        public async Task<IActionResult> Reconnect(Guid guid)
        {
            var model = GetModelForGuid(guid);
            var connectedNodes = GenerateRandomConnectedNodes(model);

            await SendDisconnectToModel(model);
            await SendConnectToModel(model, connectedNodes);

            return RedirectToAction(nameof(Details), new { guid = guid });
        }

        internal static IEnumerable<NodeModel> GenerateRandomConnectedNodes(NodeModel model)
        {
            var otherNodes = Nodes.ToArray().Where(kvp => kvp.Key != model.Guid).Select(kvp => new NodeModel { Guid = kvp.Key, Hostname = kvp.Value.Item1, Port = kvp.Value.Item2 }).ToList();

            var connectedNodes = new List<NodeModel>(otherNodes.Count);
            var modelConnectionsCount = Randomizer.Next(otherNodes.Count / 2);

            for (int i = modelConnectionsCount - 1; i >= 0; i--)
            {
                var otherNodeIdx = Randomizer.Next(otherNodes.Count);
                connectedNodes.Add(otherNodes[otherNodeIdx]);
                otherNodes.RemoveAt(otherNodeIdx);
            }

            return connectedNodes;
        }

        private static NodeModel GetModelForGuid(Guid guid)
        {
            Tuple<string, int> nodeTuple;
            if (!Nodes.TryGetValue(guid, out nodeTuple))
                throw new KeyNotFoundException($"Node with GUID {guid} unknown.");
            return new NodeModel { Guid = guid, Hostname = nodeTuple.Item1, Port = nodeTuple.Item2 };
        }

        private static async Task SendDisconnectToModel(NodeModel model)
        {
            var uriBuilder = new UriBuilder(Uri.UriSchemeHttp, model.Hostname, model.Port, "/clearConnections");
            var req = WebRequest.Create(uriBuilder.Uri) as HttpWebRequest;
            try
            {
                using (var resp = await req.GetResponseAsync() as HttpWebResponse) { }
            }
            catch (WebException) { }
        }

        internal static async Task SendConnectToModel(NodeModel model, IEnumerable<NodeModel> connectedNodes)
        {
            var uriBuilder = new UriBuilder(Uri.UriSchemeHttp, model.Hostname, model.Port, "/connectToNodes");
            var req = WebRequest.Create(uriBuilder.Uri) as HttpWebRequest;
            req.Method = WebRequestMethods.Http.Post;
            req.ContentType = new ContentType(MediaTypeNames.Text.Plain) { CharSet = Encoding.ASCII.WebName }.ToString();
            var reqData = Encoding.ASCII.GetBytes(string.Join(Environment.NewLine, connectedNodes.Select(n => $"{n.Hostname}:{n.Port}")));
            req.ContentLength = reqData.LongLength;
            try
            {
                using (var reqStream = await req.GetRequestStreamAsync())
                {
                    await reqStream.WriteAsync(reqData, 0, reqData.Length);
                    await reqStream.FlushAsync();
                }
                using (var resp = await req.GetResponseAsync() as HttpWebResponse) { }
            }
            catch (WebException) { }
        }
    }
}
