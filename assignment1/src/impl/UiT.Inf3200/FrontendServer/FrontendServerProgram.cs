using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Web;
using System.Threading;
using System.Xml.Serialization;
using THNETII;

namespace UiT.Inf3200.FrontendServer
{
    class FrontendServerProgram
    {
        private static HttpListener httpListener = new HttpListener();

        private static ConcurrentDictionary<Guid, Uri> storageNodes = new ConcurrentDictionary<Guid, Uri>();
        private static ConcurrentDictionary<Guid, Uri> logoffsInProgress = new ConcurrentDictionary<Guid, Uri>();
        private static ConcurrentDictionary<int, Guid> nodeRing = new ConcurrentDictionary<int, Guid>();

        static void Main(string[] args)
        {
            AssemblySplash.WriteAssemblySplash();
            Console.WriteLine();

            httpListener.Prefixes.Add("http://+:8181/");

            httpListener.Start();

            var kvpAr = httpListener.BeginGetContext(HandleHttpCtxCallback, 0U);

            Console.WriteLine("FRONTEND: Server started and ready to accept requests . . .");

            ConsoleTools.WriteKeyPressForExit();

            httpListener.Stop();

            Console.WriteLine("Waiting one second for all connection threads to gracefully terminate . . .");
            Thread.Sleep(1000);
        }

        private static void HandleHttpCtxCallback(IAsyncResult ar)
        {
            HttpListenerContext httpCtx;
            try { httpCtx = httpListener.EndGetContext(ar); }
            catch (ObjectDisposedException) { return; }
            catch (HttpListenerException) { return; }

            if (httpListener.IsListening)
            {
                try { httpListener.BeginGetContext(HandleHttpCtxCallback, 1U + (uint)ar.AsyncState); }
                catch (Exception) { }
            }

            var httpMethod = httpCtx.Request.HttpMethod;
            if (string.Equals(httpMethod, WebRequestMethods.Http.Get, StringComparison.InvariantCultureIgnoreCase))
            {
                HandleKvpGet(httpCtx, (uint)ar.AsyncState);
            }
            else if (string.Equals(httpMethod, WebRequestMethods.Http.Put, StringComparison.InvariantCultureIgnoreCase))
            {
                HandleKvpPut(httpCtx, (uint)ar.AsyncState);
            }
            else if (string.Equals(httpMethod, "MANAGE", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleMngCtx(httpCtx, (uint)ar.AsyncState);
            }
            else if (string.Equals(httpMethod, "DIAG", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleDiagnostics(httpCtx, (uint)ar.AsyncState);
            }
            else
            {
                Console.WriteLine("FRONTEND: [{0}] Intercepted HTTP request with unknown method: {1}", (uint)ar.AsyncState, httpMethod);

                httpCtx.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                httpCtx.Response.Close(new byte[0], willBlock: false);
            }
        }

        private static void HandleDiagnostics(HttpListenerContext httpCtx, uint httpReqId)
        {
            Console.WriteLine("FRONTEND: [{0}] Handling Diagnostics request from client {1}", httpReqId, httpCtx.Request.RemoteEndPoint);
            Console.WriteLine("FRONTEND: [{0}] Reading current ring setup", httpReqId);
            RingNode[] ringNodeArray;
            if (nodeRing.IsEmpty)
                ringNodeArray = new RingNode[0];
            else
                ringNodeArray = nodeRing.ToArray().Select(kvp => new RingNode { RingId = kvp.Key, NodeGuid = kvp.Value, NodeUri = storageNodes[kvp.Value].ToString() }).ToArray();
            var ringNodeSerializer = new XmlSerializer(ringNodeArray.GetType(), new XmlRootAttribute { ElementName = "Ring" });

            httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
            httpCtx.Response.ContentType = MediaTypeNames.Text.Xml;
            using (var targetStream = new MemoryStream())
            {
                Console.WriteLine("FRONTEND: [{0}] Serializing ring status to XML", httpReqId);

                ringNodeSerializer.Serialize(targetStream, ringNodeArray);
                targetStream.Flush();

                Console.WriteLine("FRONTEND: [{0}] Sending XMl response to client", httpReqId);
                httpCtx.Response.Close(targetStream.ToArray(), willBlock: false);
            }
        }

        private static void HandleKvpGet(HttpListenerContext httpCtx, uint httpReqId)
        {
            Console.WriteLine("FRONTEND: [{0}] Handling Key GET request from client {1}", httpReqId, httpCtx.Request.RemoteEndPoint);
            var key = httpCtx.Request.Url.LocalPath;
            bool foundNodeUri;
            Uri nodeUri;

            Console.WriteLine("FRONTEND: [{0}] Determining storage node for key {1} (Hash code: {2})", httpReqId, key, key.GetHashCode());
            do
            {
                nodeUri = FindStorageNode(key.GetHashCode(), out foundNodeUri);
            } while (!foundNodeUri);

            Console.WriteLine("FRONTEND: [{0}] Requesting key from storage node at {1}", httpReqId, nodeUri);
            var request = WebRequest.Create(new Uri(nodeUri, key));
            request.Method = WebRequestMethods.Http.Get;

            request.BeginGetResponse(ar =>
            {
                var paramArray = ar.AsyncState as object[];
                var ctx = paramArray[0] as HttpListenerContext;
                var srcReqId = (uint)paramArray[1];
                var req = paramArray[2] as WebRequest;

                using (var resp = req.EndGetResponse(ar))
                {
                    Console.WriteLine("FRONTEND: [{0}] Got response from storage node, transmittid key value to client . . .", srcReqId, nodeUri);

                    ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                    ctx.Response.ContentLength64 = resp.ContentLength;
                    ctx.Response.ContentType = resp.ContentType;

                    using (var targetStream = ctx.Response.OutputStream)
                    {
                        using (var respStream = resp.GetResponseStream())
                            respStream.CopyTo(targetStream);
                        targetStream.Flush();
                    }
                }
            }, new object[] { httpCtx, httpReqId, request });
        }

        private static void HandleKvpPut(HttpListenerContext httpCtx, uint httpReqId)
        {
            Console.WriteLine("FRONTEND: [{0}] Handling Key PUT request from client {1}", httpReqId, httpCtx.Request.RemoteEndPoint);
            var key = httpCtx.Request.Url.LocalPath;
            bool foundNodeUri;
            Uri nodeUri;
            Console.WriteLine("FRONTEND: [{0}] Determining storage node for key {1} (Hash code: {2})", httpReqId, key, key.GetHashCode());
            do
            {
                nodeUri = FindStorageNode(key.GetHashCode(), out foundNodeUri);
            } while (!foundNodeUri);

            Console.WriteLine("FRONTEND: [{0}] Sending key value to storage node at {1}", httpReqId, nodeUri);
            var request = WebRequest.Create(new Uri(nodeUri, key));
            request.Method = WebRequestMethods.Http.Put;
            request.ContentLength = httpCtx.Request.ContentLength64;
            request.ContentType = httpCtx.Request.ContentType;

            request.BeginGetRequestStream(ar =>
            {
                var paramArray = ar.AsyncState as object[];
                var sourceCtx = paramArray[0] as HttpListenerContext;
                var srcReqId = (uint)paramArray[1];
                var req = paramArray[2] as WebRequest;

                using (var reqStream = req.EndGetRequestStream(ar))
                    sourceCtx.Request.InputStream.CopyTo(reqStream);

                using (var resp = req.GetResponse()) { }
                Console.WriteLine("FRONTEND: [{0}] Key stored on storage node.", srcReqId);

                sourceCtx.Response.StatusCode = (int)HttpStatusCode.OK;
                sourceCtx.Response.Close(new byte[0], willBlock: false);

            }, new object[] { httpCtx, httpReqId, request });
        }

        private static Uri FindStorageNode(int hashCode, out bool success)
        {
            var keys = nodeRing.ToArray().Select(kvp => kvp.Key).ToArray();
            Array.Sort(keys);
            var nodeInfo = StorageNodeFinder.FindStorageNode(keys, hashCode, nodeRing, storageNodes, out success);
            if (nodeInfo == null)
                return null;
            return nodeInfo.Item2;
        }

        private static void HandleMngCtx(HttpListenerContext httpCtx, uint httpReqId)
        {
            var httpUrl = httpCtx.Request.Url;

            var managementRes = httpUrl.LocalPath;
            if (string.Equals(managementRes, "/logon", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleMngLogonRequest(httpCtx, httpReqId);
            }
            else if (string.Equals(managementRes, "/beginlogoff", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleMngBeginLogoff(httpCtx, httpReqId);
            }
            else if (string.Equals(managementRes, "/logoffcomplete", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleMngLogoffComplete(httpCtx, httpReqId);
            }
            else
            {
                httpCtx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                httpCtx.Response.Close(new byte[0], willBlock: false);
            }
        }

        private static void HandleMngLogonRequest(HttpListenerContext httpCtx, uint httpReqId)
        {
            Console.WriteLine("FRONTEND: [{0}] Handling Storage node logon from {1}", httpReqId, httpCtx.Request.RemoteEndPoint);
            var clientId = Guid.NewGuid();
            Console.WriteLine("FRONTEND: [{0}] Assigning GUID to storage node: {1}", httpReqId, clientId);

            var value = new UriBuilder();
            value.Scheme = Uri.UriSchemeHttp;
            value.Host = httpCtx.Request.RemoteEndPoint.Address.ToString();
            value.Port = 8181;

            storageNodes[clientId] = value.Uri;
            nodeRing[FindNewRingId()] = clientId;

            httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
            httpCtx.Response.ContentType = MediaTypeNames.Application.Octet;
            httpCtx.Response.Close(clientId.ToByteArray(), willBlock: true);
            Console.WriteLine("FRONTEND: [{0}] Assigned GUID to storage node: {1}", httpReqId, clientId);

            var ringNodeArray = nodeRing.ToArray().Select(kvp => new RingNode { RingId = kvp.Key, NodeGuid = kvp.Value, NodeUri = storageNodes[kvp.Value].ToString() }).ToArray();
            var redistributeClients = storageNodes.ToArray().Where(kvp => kvp.Key != clientId).ToArray();

            var ringNodeSerializer = new XmlSerializer(ringNodeArray.GetType(), new XmlRootAttribute { ElementName = "Ring" });

            Console.WriteLine("FRONTEND: [{0}] Serializing ring status to XML", httpReqId, clientId);
            byte[] ringNodeDataBytes;
            using (var ringNodeMemoryStream = new MemoryStream())
            {
                ringNodeSerializer.Serialize(ringNodeMemoryStream, ringNodeArray);
                ringNodeDataBytes = ringNodeMemoryStream.ToArray();
            }

            foreach (var redistClient in redistributeClients)
            {
                Console.WriteLine("FRONTEND: [{0}] Sending REDISTRIBUTE command to storage node {1} at {2}", httpReqId, redistClient.Key, redistClient.Value);
                var redistRequest = WebRequest.Create(redistClient.Value);
                redistRequest.Method = "REDISTRIBUTE";
                redistRequest.ContentType = MediaTypeNames.Text.Xml;
                redistRequest.ContentLength = ringNodeDataBytes.LongLength;

                redistRequest.BeginGetRequestStream(ar =>
                {
                    var paramArray = ar.AsyncState as object[];
                    var req = paramArray[0] as WebRequest;
                    var srcReqId = (uint)paramArray[1];
                    var dataBytes = paramArray[2] as byte[];

                    using (var reqStream = req.EndGetRequestStream(ar))
                        reqStream.Write(dataBytes, 0, dataBytes.Length);

                    using (var resp = req.GetResponse())
                    {
                        Console.WriteLine("FRONTEND: [{0}] REDISTRIBUTE command completed at {1}", srcReqId, resp.ResponseUri);
                    }
                }, new object[] { redistRequest, httpReqId, ringNodeDataBytes });
            }
        }

        private static void HandleMngBeginLogoff(HttpListenerContext httpCtx, uint httpReqId)
        {
            var nodeId = Guid.Parse(httpCtx.Request.QueryString["nodeid"]);
            Console.WriteLine("FRONTEND: [{0}] Handling Storage node logoff initiate from {1} at {2}", httpReqId, nodeId, httpCtx.Request.RemoteEndPoint);
            Uri nodeUri;
            Guid ignoreGuid;
            foreach (var ringId in nodeRing.ToArray().Where(kvp => kvp.Value == nodeId).Select(kvp => kvp.Key))
                nodeRing.TryRemove(ringId, out ignoreGuid);
            if (storageNodes.TryRemove(nodeId, out nodeUri))
            {
                var logoffId = Guid.NewGuid();

                logoffsInProgress[logoffId] = nodeUri;

                httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
                httpCtx.Response.ContentType = MediaTypeNames.Application.Octet;
                httpCtx.Response.Close(logoffId.ToByteArray(), willBlock: false);
                Console.WriteLine("FRONTEND: [{0}] Assigned logoff GUID: {1}", httpReqId, logoffId);
            }
            else
            {
                httpCtx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                httpCtx.Response.Close(new byte[0], willBlock: false);
            }
        }

        private static void HandleMngLogoffComplete(HttpListenerContext httpCtx, uint httpReqId)
        {
            var logoffId = Guid.Parse(httpCtx.Request.QueryString["logoffId"]);
            Console.WriteLine("FRONTEND: [{0}] Handling Storage node logoff completion from logoff {1} at {2}", httpReqId, logoffId, httpCtx.Request.RemoteEndPoint);
            Uri nodeUri;
            if (logoffsInProgress.TryRemove(logoffId, out nodeUri))
            {
                httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                httpCtx.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
            httpCtx.Response.Close(new byte[0], willBlock: false);
        }

        private static int FindNewRingId()
        {
            var nodeRingIds = nodeRing.ToArray().Select(kvp => kvp.Key).ToArray();
            if (nodeRingIds.Length < 1)
                return int.MinValue;
            else if (nodeRingIds.Length < 2)
                return 0;

            Array.Sort(nodeRingIds);
            var lastIdx = nodeRingIds.Length - 1;
            var maxDistance = Tuple.Create(nodeRingIds[0], nodeRingIds[lastIdx], Math.Abs(nodeRingIds[0] - nodeRingIds[lastIdx]));
            for (int i = 1; i < nodeRingIds.Length; i++)
            {
                int distance = Math.Abs(nodeRingIds[i] - nodeRingIds[i - 1]);
                if (distance > maxDistance.Item3)
                    maxDistance = Tuple.Create(nodeRingIds[i], nodeRingIds[i - 1], distance);
            }

            return maxDistance.Item2 + (maxDistance.Item3 / 2);
        }
    }
}
