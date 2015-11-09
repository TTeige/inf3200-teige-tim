using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UiT.Inf3200.P2PNode
{
    static class Program
    {
        private static readonly ReaderWriterLockSlim nodeStateLock = new ReaderWriterLockSlim();
        private static ManualResetEvent terminateSignal = new ManualResetEvent(initialState: false);
        private static Guid nodeGuid;
        private static int portNumber;

        private static readonly ConcurrentDictionary<Guid, Tuple<string, int>> outgoingNodeConnections = new ConcurrentDictionary<Guid, Tuple<string, int>>();
        private static readonly ConcurrentDictionary<Guid, Tuple<string, int>> incomingNodeConnections = new ConcurrentDictionary<Guid, Tuple<string, int>>();

        static void Main(string[] args)
        {
            THNETII.AssemblySplash.WriteAssemblySplash();

            Console.WriteLine();

            if (args == null || args.Length < 1 || !int.TryParse(args[0], out portNumber))
                portNumber = 8899;

            string host = "+";
            if (args != null && args.Length > 1)
                host = args[1];

            nodeGuid = Guid.NewGuid();

            Console.CancelKeyPress += OnTerminateSignal;

            var http_listener = new HttpListener();
            http_listener.Prefixes.Add(string.Format("http://{0}:{1}/", host, portNumber));

            http_listener.Start();
            HandleHttpContext(http_listener);

            terminateSignal.WaitOne();

            ClearConnectionsContext();

            http_listener.Stop();
        }

        private static void ClearConnectionsContext(HttpListenerContext http_ctx = null)
        {
            var connectedNodes = outgoingNodeConnections.ToArray().Concat(incomingNodeConnections.ToArray());
            var deregistrationTaskList = new List<Task>();
            foreach (var otherNodeKvp in connectedNodes)
            {
                deregistrationTaskList.Add(DeregisterNodeAtNeighbour(otherNodeKvp.Value.Item1, otherNodeKvp.Value.Item2));
            }

            Task.WaitAll(deregistrationTaskList.ToArray());

            incomingNodeConnections.Clear();
            outgoingNodeConnections.Clear();

            if (http_ctx != null)
            {
                http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                http_ctx.Response.Close();
            }
        }

        private static async Task DeregisterNodeAtNeighbour(string hostname, int port)
        {
            var uriBuilder = new UriBuilder(Uri.UriSchemeHttp, hostname, port, "/deregisterNode");
            var req = WebRequest.Create(uriBuilder.Uri) as HttpWebRequest;
            req.Method = WebRequestMethods.Http.Post;
            var contentTypeObj = new ContentType(MediaTypeNames.Text.Plain);
            contentTypeObj.CharSet = Encoding.ASCII.WebName;
            req.ContentType = contentTypeObj.ToString();

            var reqBodyData = Encoding.ASCII.GetBytes(nodeGuid.ToString());
            req.ContentLength = reqBodyData.LongLength;

            using (var reqStream = await req.GetRequestStreamAsync())
            {
                await reqStream.WriteAsync(reqBodyData, 0, reqBodyData.Length);
                await reqStream.FlushAsync();
            }

            using (var resp = await req.GetResponseAsync()) { }
        }

        private static void OnTerminateSignal(object sender, ConsoleCancelEventArgs e)
        {
            if (e.SpecialKey == ConsoleSpecialKey.ControlC)
            {
                e.Cancel = terminateSignal.Set();
            }
        }

        private static async void HandleHttpContext(HttpListener http_listener)
        {
            HttpListenerContext http_ctx;
            try { http_ctx = await http_listener.GetContextAsync(); }
            catch (Exception) { return; }

            //nodeStateLock.EnterReadLock();

            try
            {
                HandleHttpContext(http_listener);

                var path = http_ctx.Request.Url.AbsolutePath;
                if (string.Equals("/getCurrentLeader", path, StringComparison.OrdinalIgnoreCase))
                {
                    http_ctx.Response.ContentType = MediaTypeNames.Text.Plain;
                    http_ctx.Response.ContentEncoding = Encoding.ASCII;
                    http_ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    http_ctx.Response.Close(Encoding.ASCII.GetBytes("The requested resource could not be found."), willBlock: false);
                }
                else if (string.Equals("/getNodes", path, StringComparison.OrdinalIgnoreCase))
                {
                    GetNodesContext(http_ctx);
                }
                else if (string.Equals("/getNodesWithGuids", path, StringComparison.OrdinalIgnoreCase))
                {
                    GetNodesContext(http_ctx, withGuids: true);
                }
                else if (string.Equals("/getGuid", path, StringComparison.OrdinalIgnoreCase))
                {
                    GetGuidContext(http_ctx);
                }
                else if (string.Equals("/connectToNodes", path, StringComparison.OrdinalIgnoreCase))
                {
                    await ConnectToNodesContext(http_ctx);
                }
                else if (string.Equals("/registerNeighbour", path, StringComparison.OrdinalIgnoreCase))
                {
                    RegisterNeighbourContext(http_ctx);
                }
                else if (string.Equals("/clearConnections", path, StringComparison.OrdinalIgnoreCase))
                {
                    ClearConnectionsContext(http_ctx);
                }
                else if (string.Equals("/deregisterNode", path, StringComparison.OrdinalIgnoreCase))
                {
                    await DeregisterNodeContext(http_ctx);
                }
                else
                {
                    http_ctx.Response.ContentType = MediaTypeNames.Text.Plain;
                    http_ctx.Response.ContentEncoding = Encoding.ASCII;
                    http_ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    http_ctx.Response.Close(Encoding.ASCII.GetBytes("The requested resource could not be found."), willBlock: false);
                }
            }
            finally
            {
                //nodeStateLock.ExitReadLock();
            }
        }

        private static void GetGuidContext(HttpListenerContext http_ctx)
        {
            http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            http_ctx.Response.ContentType = MediaTypeNames.Text.Plain;
            http_ctx.Response.ContentEncoding = Encoding.ASCII;
            http_ctx.Response.Close(Encoding.ASCII.GetBytes(nodeGuid.ToString()), willBlock: false);
        }

        private static void GetNodesContext(HttpListenerContext http_ctx, bool withGuids = false)
        {
            http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            http_ctx.Response.ContentType = MediaTypeNames.Text.Plain;
            http_ctx.Response.ContentEncoding = Encoding.ASCII;
            using (var respWriter = new StreamWriter(http_ctx.Response.OutputStream))
            {
                foreach (var connectedNodeKvp in outgoingNodeConnections.ToArray().Concat(incomingNodeConnections.ToArray()))
                {
                    if (withGuids)
                        respWriter.WriteLine(string.Format("{0} {1}:{2}", connectedNodeKvp.Key, connectedNodeKvp.Value.Item1, connectedNodeKvp.Value.Item2));
                    else
                        respWriter.WriteLine(string.Format("{0}:{1}", connectedNodeKvp.Value.Item1, connectedNodeKvp.Value.Item2));
                }
                respWriter.Flush();
            }
            http_ctx.Response.Close();
        }

        private static async Task DeregisterNodeContext(HttpListenerContext http_ctx)
        {
            var reqEncoding = http_ctx.Request.ContentEncoding ?? Encoding.ASCII;
            Guid otherGuid;
            using (var reqReader = new StreamReader(http_ctx.Request.InputStream, reqEncoding))
                otherGuid = Guid.Parse(await reqReader.ReadLineAsync());
            Tuple<string, int> ignoreTuple;
            outgoingNodeConnections.TryRemove(otherGuid, out ignoreTuple);
            incomingNodeConnections.TryRemove(otherGuid, out ignoreTuple);

            http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            http_ctx.Response.Close(new byte[0], willBlock: false);
        }

        private static async Task ConnectToNodesContext(HttpListenerContext http_ctx)
        {
            var contentTypeObj = new ContentType(MediaTypeNames.Text.Plain);
            contentTypeObj.CharSet = Encoding.ASCII.WebName;
            var contentTypeString = contentTypeObj.ToString();
            var reqBodyData = Encoding.ASCII.GetBytes(GetNodeSpecString(http_ctx));

            var registrationTaksList = new List<Task>();
            using (var bodyReader = new StreamReader(http_ctx.Request.InputStream, http_ctx.Request.ContentEncoding))
            {
                var bodyLineTask = bodyReader.ReadLineAsync();
                for (var bodyLine = await bodyLineTask; bodyLine != null; bodyLine = await bodyLineTask)
                {
                    bodyLineTask = bodyReader.ReadLineAsync();

                    var nodeAddr = bodyLine;
                    var colIdx = nodeAddr.LastIndexOf(':');
                    UriBuilder uriBuilder;
                    if (colIdx < 0)
                        uriBuilder = new UriBuilder(Uri.UriSchemeHttp, nodeAddr);
                    else
                        uriBuilder = new UriBuilder(Uri.UriSchemeHttp, nodeAddr.Substring(0, colIdx), int.Parse(nodeAddr.Substring(colIdx + 1)));

                    registrationTaksList.Add(ExchangeWithNode(uriBuilder.Uri, reqBodyData, contentTypeString));
                }
            }

            await Task.WhenAll(registrationTaksList);
            http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            http_ctx.Response.Close(new byte[0], willBlock: false);
        }

        private static async Task ExchangeWithNode(Uri otherNodeUri, byte[] nodeSpecData, string contentType)
        {
            var req = WebRequest.Create(new Uri(otherNodeUri, "/registerNeighbour")) as HttpWebRequest;
            req.Method = WebRequestMethods.Http.Post;
            req.ContentType = contentType;
            req.ContentLength = nodeSpecData.Length;
            using (var reqStream = await req.GetRequestStreamAsync())
            {
                await reqStream.WriteAsync(nodeSpecData, 0, nodeSpecData.Length);
                await reqStream.FlushAsync();
            }

            Guid otherGuid; string otherHostname; int otherPort;
            using (var resp = await req.GetResponseAsync() as HttpWebResponse)
            {
                var respEncoding = string.IsNullOrWhiteSpace(resp.ContentEncoding) ? Encoding.ASCII : Encoding.GetEncoding(resp.ContentEncoding);
                using (var respReader = new StreamReader(resp.GetResponseStream(), respEncoding))
                {
                    otherGuid = Guid.Parse(await respReader.ReadLineAsync());
                    otherHostname = await respReader.ReadLineAsync();
                    otherPort = int.Parse(await respReader.ReadLineAsync());
                }

            }

            if (nodeGuid.CompareTo(otherGuid) < 0)
                outgoingNodeConnections[otherGuid] = Tuple.Create(otherHostname, otherPort);
            else
                incomingNodeConnections[otherGuid] = Tuple.Create(otherHostname, otherPort);
        }

        private static void RegisterNeighbourContext(HttpListenerContext http_ctx)
        {
            Guid otherGuid; string otherHostname; int otherPort;
            using (var bodyReader = new StreamReader(http_ctx.Request.InputStream, http_ctx.Request.ContentEncoding ?? Encoding.ASCII))
            {
                otherGuid = Guid.Parse(bodyReader.ReadLine());
                otherHostname = bodyReader.ReadLine();
                otherPort = int.Parse(bodyReader.ReadLine());
            }

            if (nodeGuid.CompareTo(otherGuid) < 0)
                outgoingNodeConnections[otherGuid] = Tuple.Create(otherHostname, otherPort);
            else
                incomingNodeConnections[otherGuid] = Tuple.Create(otherHostname, otherPort);

            var respBodyString = GetNodeSpecString(http_ctx);
            var repsBodyData = Encoding.ASCII.GetBytes(respBodyString);
            http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            http_ctx.Response.ContentType = MediaTypeNames.Text.Plain;
            http_ctx.Response.ContentEncoding = Encoding.ASCII;
            http_ctx.Response.Close(repsBodyData, willBlock: false);
        }

        private static string GetNodeSpecString(HttpListenerContext http_ctx)
        {
            var nodeSpecBuilder = new StringBuilder();
            nodeSpecBuilder
                .AppendLine(nodeGuid.ToString())
                .AppendLine(http_ctx.Request.LocalEndPoint.Address.ToString())
                .Append(http_ctx.Request.LocalEndPoint.Port);
            return nodeSpecBuilder.ToString();
        }
    }
}
