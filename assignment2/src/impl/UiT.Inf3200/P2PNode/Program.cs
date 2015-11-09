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

        private static readonly ConcurrentDictionary<Guid, LeaderElectionState> leaderElections = new ConcurrentDictionary<Guid, LeaderElectionState>();
        private static readonly ConcurrentDictionary<Guid, NetworkNode> outgoingNodeConnections = new ConcurrentDictionary<Guid, NetworkNode>();
        private static readonly ConcurrentDictionary<Guid, NetworkNode> incomingNodeConnections = new ConcurrentDictionary<Guid, NetworkNode>();
        private static Guid? leaderGuid;

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
                deregistrationTaskList.Add(DeregisterNodeAtNeighbour(otherNodeKvp.Value.Hostname, otherNodeKvp.Value.Port));
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
                    var inNodeKvps = incomingNodeConnections.ToArray();
                    var outNodeKvps = outgoingNodeConnections.ToArray();

                    var newElectionState = new LeaderElectionState(inNodeKvps.Where(kvp => !kvp.Value.IsUselessConnection).Select(kvp => kvp.Key),
                            outNodeKvps.Where(kvp => !kvp.Value.IsUselessConnection).Select(kvp => kvp.Key))
                    { Guid = Guid.NewGuid(), CreationTime = DateTime.Now, FirstYoMinimum = nodeGuid };

                    leaderElections[newElectionState.Guid] = newElectionState;

                    SendLeaderElectionAdvertisementRequest(newElectionState, new NetworkNode { Hostname = "localhost", Port = portNumber }).Wait();

                    Guid? currentLeader;
                    do
                    {
                        Task.Delay(42).Wait();
                        currentLeader = leaderGuid;
                    } while (currentLeader == null);

                    NetworkNode leaderNode;
                    if (currentLeader == nodeGuid)
                        leaderNode = new NetworkNode { Hostname = http_ctx.Request.LocalEndPoint.Address.ToString(), Port = portNumber };
                    else if (!incomingNodeConnections.TryGetValue(currentLeader.GetValueOrDefault(), out leaderNode))
                    {
                        http_ctx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        http_ctx.Response.Close();
                    }

                    http_ctx.Response.ContentType = MediaTypeNames.Text.Plain;
                    http_ctx.Response.ContentEncoding = Encoding.ASCII;
                    http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                    http_ctx.Response.Close(Encoding.ASCII.GetBytes(string.Format("{0}:{1}", leaderNode.Hostname, leaderNode.Port)), willBlock: false);
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
                else if (string.Equals("/firstYo/Advertise", path, StringComparison.OrdinalIgnoreCase))
                {
                    FirstYoAdvertisementContext(http_ctx);
                }
                else if (string.Equals("/firstYo/RequestAdvertisement", path, StringComparison.OrdinalIgnoreCase))
                {
                    FirstYoRequestAdvertisementContext(http_ctx);
                }
                else if (string.Equals("/secondYo/Response", path, StringComparison.OrdinalIgnoreCase))
                {
                    SecondYoResponseContext(http_ctx);
                }
                else if (string.Equals("/secondYo/Flip", path, StringComparison.OrdinalIgnoreCase))
                {
                    SecondYoFlipContext(http_ctx);
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

        private static void SecondYoFlipContext(HttpListenerContext http_ctx)
        {
            Guid nodeGuid;
            using (var reqReader = new StreamReader(http_ctx.Request.InputStream))
            {
                nodeGuid = Guid.Parse(reqReader.ReadLine());
            }

            NetworkNode flipTuple;
            outgoingNodeConnections.TryRemove(nodeGuid, out flipTuple);
            incomingNodeConnections.TryAdd(nodeGuid, flipTuple);

            flipTuple.IsUselessConnection = false;

            http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            http_ctx.Response.Close();
        }

        private static async Task SendSecondYoFlipNode(NetworkNode nodeTuple)
        {
            var uriBuilder = new UriBuilder(Uri.UriSchemeHttp, nodeTuple.Hostname, nodeTuple.Port, "/secondYo/Flip");
            var req = WebRequest.Create(uriBuilder.Uri) as HttpWebRequest;
            req.Method = WebRequestMethods.Http.Post;
            req.ContentType = new ContentType(MediaTypeNames.Text.Plain) { CharSet = Encoding.ASCII.WebName }.ToString();
            var reqDataString = nodeGuid.ToString() + Environment.NewLine;
            var reqDataBytes = Encoding.ASCII.GetBytes(reqDataString);
            req.ContentLength = reqDataBytes.LongLength;
            try
            {
                using (var reqStream = await req.GetRequestStreamAsync())
                {
                    await reqStream.WriteAsync(reqDataBytes, 0, reqDataBytes.Length);
                    await reqStream.FlushAsync();
                }
                using (var resp = await req.GetResponseAsync()) { }
            }
            catch (WebException) { }
        }

        private static void SecondYoResponseContext(HttpListenerContext http_ctx)
        {
            // Received response to leader election advertisement

            Guid electionGuid; DateTime electionTime; Guid nodeGuid; bool responseState, uniqueState, uselessState;
            using (var reqReader = new StreamReader(http_ctx.Request.InputStream))
            {
                electionGuid = Guid.Parse(reqReader.ReadLine());
                electionTime = DateTime.Parse(reqReader.ReadLine());
                nodeGuid = Guid.Parse(reqReader.ReadLine());
                responseState = bool.Parse(reqReader.ReadLine());
                uniqueState = bool.Parse(reqReader.ReadLine());
                uselessState = bool.Parse(reqReader.ReadLine());
            }

            http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            http_ctx.Response.Close();

            LeaderElectionState electionState;
            if (!leaderElections.TryGetValue(electionGuid, out electionState))
                return; // Ongoing Leader election unknown. Disregarding

            // Registering response
            electionState.SecondYoResponses[nodeGuid] = responseState;

            var responseKvps = electionState.SecondYoResponses.ToArray();

            if (responseKvps.All(kvp => kvp.Value != null))
            {
                // All responses have been received

                if (responseKvps.Any(kvp => kvp.Value == false))
                {
                    // If any response was negative
                    // all further responses will be negative

                    // Propagating negative responses back over incoming connections
                    Task.WaitAll(incomingNodeConnections.ToArray().Select(kvp =>
                        SendLeaderElectionResponse(electionState, kvp.Value, false, false, false)).ToArray());
                }
                else
                {
                    // All responses were positive

                    // Propagating responses back over incoming connections
                    // accoring to match with determined minimum
                    var responseTaskList = new List<Task>();

                    var receivedAdvertSet = new HashSet<Guid>();
                    var receivedAdvertKvps = electionState.FirstYoAdvertisements.ToArray();
                    foreach (var receivedAdvertKvp in receivedAdvertKvps)
                    {
                        // The response is only YES if the actual minimum was received
                        var subResponseState = receivedAdvertKvp.Value == electionState.FirstYoMinimum;

                        // Only the first of all received values was a unique advertisement
                        var uniqueAdvert = receivedAdvertSet.Add(receivedAdvertKvp.Value.GetValueOrDefault());

                        var receivedNodeTuple = incomingNodeConnections[receivedAdvertKvp.Key];

                        var subUselessState = electionState.SecondYoResponses.IsEmpty && electionState.FirstYoAdvertisements.Count < 2;

                        // Sending Response back to incoming connection
                        responseTaskList.Add(SendLeaderElectionResponse(electionState, receivedNodeTuple, subResponseState, uniqueAdvert, subUselessState));
                    }
                }

                var flipTaskList = new List<Task>();
                foreach (var flipGuid in responseKvps.Where(kvp => kvp.Value == false).Select(kvp => kvp.Key))
                {
                    // Flipping the direction of all negative responded connections
                    NetworkNode flipTuple;
                    if (!outgoingNodeConnections.TryRemove(flipGuid, out flipTuple))
                        continue;
                    else if (!incomingNodeConnections.TryAdd(flipGuid, flipTuple))
                        continue;

                    // Transmitting flip directive to the other node so it does the same
                    flipTaskList.Add(SendSecondYoFlipNode(flipTuple));
                }
                Task.WaitAll(flipTaskList.ToArray());

                var inNodeKvps = incomingNodeConnections.ToArray();
                var outNodeKvps = outgoingNodeConnections.ToArray();

                if (inNodeKvps.Length == 0)
                {
                    if (outNodeKvps.Select(kvp => kvp.Value).All(node => node.IsUselessConnection))
                    {
                        // Only useless outgoing connections and no incoming connections
                        // This node is the leader. Leader election complete

                        leaderGuid = electionState.FirstYoMinimum;
                    }
                    else
                    {
                        var newElectionState = new LeaderElectionState(inNodeKvps.Where(kvp => !kvp.Value.IsUselessConnection).Select(kvp => kvp.Key),
                            outNodeKvps.Where(kvp => !kvp.Value.IsUselessConnection).Select(kvp => kvp.Key))
                        { Guid = Guid.NewGuid(), CreationTime = DateTime.Now, FirstYoMinimum = nodeGuid };

                        leaderElections[newElectionState.Guid] = newElectionState;
                        leaderElections.TryRemove(electionGuid, out electionState);

                        Task.WaitAll(outNodeKvps.Select(kvp => SendLeaderElectionAdvertisement(newElectionState, kvp.Value, nodeGuid)).ToArray());
                    }
                }
            }
        }

        private static void FirstYoRequestAdvertisementContext(HttpListenerContext http_ctx)
        {
            // Received to request to initiate YO-YO leader election
            // Request should be propaged along all currently inward pointing edges
            // since this request only should be received along outgoing edges
            // => Leader election initiation request travel along the network graph in reverse edge order

            leaderGuid = null;

            LeaderElectionState electionState;

            var inNodeKvps = incomingNodeConnections.ToArray();
            var outNodeKvps = outgoingNodeConnections.ToArray();

            using (var reqReader = new StreamReader(http_ctx.Request.InputStream))
            {
                var electionGuid = Guid.Parse(reqReader.ReadLine());
                var electionTime = DateTime.Parse(reqReader.ReadLine());

                // Create a new leader election state with all incoming node connections
                electionState = new LeaderElectionState(inNodeKvps.Select(kvp => kvp.Key),
                    outNodeKvps.Select(kvp => kvp.Key))
                { Guid = electionGuid, CreationTime = electionTime };

                // Add the leader election state to current leader elections
                leaderElections[electionGuid] = electionState;
            }

            // Leader election initiation received
            http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            http_ctx.Response.Close();


            var inNodeTuples = inNodeKvps.Select(kvp => kvp.Value);
            if (inNodeTuples.Any()) // If there are inward pointing connections to the current, propagate leader election initiation request
                Task.WaitAll(inNodeTuples.Select(n => SendLeaderElectionAdvertisementRequest(electionState, n)).ToArray());
            else
            {
                // No inward pointing connections, start leader election
                var outNodeTuples = outNodeKvps.Select(kvp => kvp.Value);

                // Advertise current node GUID to all outgoing connections
                Task.WaitAll(outNodeTuples.Select(n => SendLeaderElectionAdvertisement(electionState, n, nodeGuid)).ToArray());
            }
        }

        private static Task SendLeaderElectionAdvertisementRequest(LeaderElectionState electionState, NetworkNode nodeTuple)
        {
            var uriBuilder = new UriBuilder(Uri.UriSchemeHttp, nodeTuple.Hostname, nodeTuple.Port, "/firstYo/RequestAdvertisement");
            var reqDataString = string.Join(Environment.NewLine, electionState.Guid, electionState.CreationTime.ToString("o"));
            var reqDataBytes = Encoding.ASCII.GetBytes(reqDataString);
            return SendNotificationPostRequestToNode(uriBuilder.Uri, reqDataBytes);
        }

        private static Task SendLeaderElectionAdvertisement(LeaderElectionState electionState, NetworkNode nodeTuple, Guid advertGuid)
        {
            var uriBuilder = new UriBuilder(Uri.UriSchemeHttp, nodeTuple.Hostname, nodeTuple.Port, "/firstYo/Advertise");
            var reqDataString = string.Join(Environment.NewLine, electionState.Guid, electionState.CreationTime.ToString("o"), nodeGuid, advertGuid);
            var reqDataBytes = Encoding.ASCII.GetBytes(reqDataString);
            return SendNotificationPostRequestToNode(uriBuilder.Uri, reqDataBytes);
        }

        private static async Task SendNotificationPostRequestToNode(Uri nodeUri, byte[] reqData)
        {
            var req = WebRequest.Create(nodeUri) as HttpWebRequest;
            req.Method = WebRequestMethods.Http.Post;
            req.ContentType = new ContentType(MediaTypeNames.Text.Plain) { CharSet = Encoding.ASCII.WebName }.ToString();
            req.ContentLength = reqData.LongLength;
            try
            {
                using (var reqStream = await req.GetRequestStreamAsync())
                {
                    await reqStream.WriteAsync(reqData, 0, reqData.Length);
                    await reqStream.FlushAsync();
                }
                using (var resp = await req.GetResponseAsync()) { }
            }
            catch (WebException) { }
        }

        private static void FirstYoAdvertisementContext(HttpListenerContext http_ctx)
        {
            // Received leader election state from incoming connection

            leaderGuid = null;

            LeaderElectionState electionState;
            Guid electionGuid; DateTime electionTime; Guid nodeGuid, advertGuid;
            using (var reqReader = new StreamReader(http_ctx.Request.InputStream))
            {
                electionGuid = Guid.Parse(reqReader.ReadLine());
                electionTime = DateTime.Parse(reqReader.ReadLine());
                nodeGuid = Guid.Parse(reqReader.ReadLine());
                advertGuid = Guid.Parse(reqReader.ReadLine());
            }

            http_ctx.Response.StatusCode = (int)HttpStatusCode.OK;
            http_ctx.Response.Close();

            var inNodeKvps = incomingNodeConnections.ToArray();
            var outNodeKvps = outgoingNodeConnections.ToArray();

            // Attempt to find the leader election state in the ongoing leader election dict
            if (!leaderElections.TryGetValue(electionGuid, out electionState))
            {
                // Ongoing leader election unknown, registering implicit request for leader election initiation
                electionState = new LeaderElectionState(inNodeKvps.Select(kvp => kvp.Key), outNodeKvps.Select(kvp => kvp.Key))
                { Guid = electionGuid, CreationTime = electionTime };

                if (!leaderElections.TryAdd(electionGuid, electionState))
                    electionState = leaderElections[electionGuid];

                // Propagating leader election initiation request to all inward pointing connections
                // to ensure advertisements from all ingoing connections
                Task.WaitAll(inNodeKvps.Select(kvp => SendLeaderElectionAdvertisementRequest(electionState, kvp.Value)).ToArray());
            }

            // Store advertised GUID in the list of expected advertisements
            electionState.FirstYoAdvertisements[nodeGuid] = advertGuid;

            var firstYoAdvertKvps = electionState.FirstYoAdvertisements.ToArray();

            if (firstYoAdvertKvps.All(kvp => kvp.Value != null))
            {
                // All outstaning advertisements received.
                // Ready to propagte minimum to all outgoing connections

                var minGuid = firstYoAdvertKvps.Min(kvp => kvp.Key);
                electionState.FirstYoMinimum = minGuid;

                leaderGuid = minGuid;

                var outNodes = outNodeKvps.Select(kvp => kvp.Value);
                if (outNodes.Any())
                    Task.WaitAll(outNodes.Select(n => SendLeaderElectionAdvertisement(electionState, n, nodeGuid)).ToArray());
                else
                {
                    // No outgoing connections -> current node is a sink
                    // Sending election responses back to incoming connections

                    var responseTaskList = new List<Task>();

                    var receivedAdvertSet = new HashSet<Guid>();
                    var receivedAdvertKvps = electionState.FirstYoAdvertisements.ToArray();
                    foreach (var receivedAdvertKvp in receivedAdvertKvps)
                    {
                        // The response is only YES if the actual minimum was received
                        var responseState = receivedAdvertKvp.Value == electionState.FirstYoMinimum;

                        // Only the first of all received values was a unique advertisement
                        var uniqueAdvert = receivedAdvertSet.Add(receivedAdvertKvp.Value.GetValueOrDefault());

                        var receivedNodeTuple = incomingNodeConnections[receivedAdvertKvp.Key];

                        var uselessState = firstYoAdvertKvps.Length < 2;

                        // Sending Response back to incoming connection
                        responseTaskList.Add(SendLeaderElectionResponse(electionState, receivedNodeTuple, responseState, uniqueAdvert, uselessState));
                    }
                }
            }
            else if ((DateTime.Now - electionState.CreationTime) > new TimeSpan(0, 0, 8))
            {
                // Retransmitting leader election initiation request after timeout
                var inKvps = electionState.FirstYoAdvertisements.ToArray().Where(kvp => kvp.Value == null);
                Task.WaitAll(inKvps.Select(kvp => SendLeaderElectionAdvertisementRequest(electionState, incomingNodeConnections[kvp.Key])).ToArray());
            }
        }

        private static Task SendLeaderElectionResponse(LeaderElectionState electionState, NetworkNode nodeTuple, bool responseState, bool uniqueState, bool uselessState)
        {
            var uriBuilder = new UriBuilder(Uri.UriSchemeHttp, nodeTuple.Hostname, nodeTuple.Port, "/secondYo/Response");
            var reqDataString = string.Join(Environment.NewLine, 
                electionState.Guid, electionState.CreationTime.ToString("o"), nodeGuid, responseState, uniqueState, uselessState);
            var reqDataBytes = Encoding.ASCII.GetBytes(reqDataString);
            return SendNotificationPostRequestToNode(uriBuilder.Uri, reqDataBytes);
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
                        respWriter.WriteLine(string.Format("{0} {1}:{2}", connectedNodeKvp.Key, connectedNodeKvp.Value.Hostname, connectedNodeKvp.Value.Port));
                    else
                        respWriter.WriteLine(string.Format("{0}:{1}", connectedNodeKvp.Value.Hostname, connectedNodeKvp.Value.Port));
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
            NetworkNode ignoreTuple;
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
                outgoingNodeConnections[otherGuid] = new NetworkNode { Hostname = otherHostname, Port = otherPort };
            else
                incomingNodeConnections[otherGuid] = new NetworkNode { Hostname = otherHostname, Port = otherPort };
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
                outgoingNodeConnections[otherGuid] = new NetworkNode { Hostname = otherHostname, Port = otherPort };
            else
                incomingNodeConnections[otherGuid] = new NetworkNode { Hostname = otherHostname, Port = otherPort };

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
