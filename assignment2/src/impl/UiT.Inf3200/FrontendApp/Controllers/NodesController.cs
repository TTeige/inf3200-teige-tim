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

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace UiT.Inf3200.FrontendApp.Controllers
{
    public class NodesController : Controller
    {
        //public static ConcurrentBag<Tuple<string, int>> Nodes { get; } = new ConcurrentBag<Tuple<string, int>>();
        public static ConcurrentDictionary<Guid, Tuple<string, int>> Nodes { get; } = new ConcurrentDictionary<Guid, Tuple<string, int>>();

        // GET: /<controller>/
        public IActionResult Index()
        {
            var nodes = Nodes.ToArray().Select(kvp => new NodeModel { Guid = kvp.Key, Hostname = kvp.Value.Item1, Port = kvp.Value.Item2 }).ToArray();

            return View(nodes);
        }

        public IActionResult Details(Guid guid)
        {
            Tuple<string, int> nodeTuple;
            if (!Nodes.TryGetValue(guid, out nodeTuple))
                return HttpNotFound(new KeyNotFoundException($"Node with GUID {guid} unknown."));
            var model = new NodeModel { Guid = guid, Hostname = nodeTuple.Item1, Port = nodeTuple.Item2 };

            return View(model);
        }
    }
}
