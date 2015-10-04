﻿using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.Serialization.Json;
using System.ServiceModel.Web;
using System.Threading;
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
            Console.WriteLine("FRONTEND: [{0}] Waiting for a new HTTP context . . .", (uint)ar.AsyncState);
            HttpListenerContext httpCtx;
            try { httpCtx = httpListener.EndGetContext(ar); }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("FRONTEND: [{0}] Failed to receive a new HTTP context . . .", (uint)ar.AsyncState);
                return;
            }
            catch (HttpListenerException) { return; }

            if (httpListener.IsListening)
            {
                try { httpListener.BeginGetContext(HandleHttpCtxCallback, 1U + (uint)ar.AsyncState); }
                catch (Exception) { }
            }

            var httpMethod = httpCtx.Request.HttpMethod;
            if (string.Equals(httpMethod, WebRequestMethods.Http.Get, StringComparison.InvariantCultureIgnoreCase))
            {
                HandleKvpGet(httpCtx);
            }
            else if (string.Equals(httpMethod, WebRequestMethods.Http.Put, StringComparison.InvariantCultureIgnoreCase))
            {
                HandleKvpPut(httpCtx);
            }
            else if (string.Equals(httpMethod, "MANAGE", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleMngCtx(httpCtx);
            }
            else if (string.Equals(httpMethod, "DIAG", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleDiagnostics(httpCtx);
            }
            else
            {
                Console.WriteLine("FRONTEND: [{0}] Intercepted HTTP request with unknown method: {1} . . .", (uint)ar.AsyncState, httpMethod);

                httpCtx.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                httpCtx.Response.Close(new byte[0], willBlock: false);
            }
        }

        private static void HandleDiagnostics(HttpListenerContext httpCtx)
        {
            var ringNodeUriDict = nodeRing.ToDictionary(nodeKvp => nodeKvp.Key, nodeKvp => new[] { nodeKvp.Value.ToString(), storageNodes[nodeKvp.Value].ToString() });
            var ringNodeSerializer = new DataContractJsonSerializer(ringNodeUriDict.GetType(),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

            httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
            httpCtx.Response.ContentType = "application/json";
            using (var targetStream = httpCtx.Response.OutputStream)
                ringNodeSerializer.WriteObject(targetStream, ringNodeUriDict); 
        }

        private static void HandleKvpGet(HttpListenerContext httpCtx)
        {
            var key = httpCtx.Request.Url.LocalPath;
            bool foundNodeUri;
            Uri nodeUri;
            do
            {
                nodeUri = FindStorageNode(key.GetHashCode(), out foundNodeUri);
            } while (!foundNodeUri);

            var request = WebRequest.Create(new Uri(nodeUri, key));
            request.Method = WebRequestMethods.Http.Get;

            request.BeginGetResponse(ar =>
            {
                var paramArray = ar.AsyncState as object[];
                var ctx = paramArray[0] as HttpListenerContext;
                var req = paramArray[1] as WebRequest;

                using (var resp = req.EndGetResponse(ar))
                {
                    ctx.Response.StatusCode = (int)HttpStatusCode.OK;
                    ctx.Response.ContentLength64 = resp.ContentLength;
                    ctx.Response.ContentType = resp.ContentType;

                    using (var targetStream = ctx.Response.OutputStream)
                    {
                        using (var respStream = resp.GetResponseStream())
                            respStream.CopyTo(targetStream); 
                    }
                }
            }, new object[] { httpCtx, request });
        }

        private static void HandleKvpPut(HttpListenerContext httpCtx)
        {
            var key = httpCtx.Request.Url.LocalPath;
            bool foundNodeUri;
            Uri nodeUri;
            do
            {
                nodeUri = FindStorageNode(key.GetHashCode(), out foundNodeUri);
            } while (!foundNodeUri);

            var request = WebRequest.Create(new Uri(nodeUri, key));
            request.Method = WebRequestMethods.Http.Put;
            request.ContentLength = httpCtx.Request.ContentLength64;
            request.ContentType = httpCtx.Request.ContentType;

            request.BeginGetRequestStream(ar =>
            {
                var paramArray = ar.AsyncState as object[];
                var sourceCtx = paramArray[0] as HttpListenerContext;
                var req = paramArray[1] as WebRequest;

                using (var reqStream = req.EndGetRequestStream(ar))
                    sourceCtx.Request.InputStream.CopyTo(reqStream);

                using (var resp = req.GetResponse()) { }

                sourceCtx.Response.StatusCode = (int)HttpStatusCode.OK;
                sourceCtx.Response.Close(new byte[0], willBlock: false);

            }, new object[] { httpCtx, request });
        }

        private static Uri FindStorageNode(int hashCode, out bool success)
        {
            var keys = nodeRing.Keys.ToArray();
            Array.Sort(keys);
            var nodeInfo = StorageNodeFinder.FindStorageNode(keys, hashCode, nodeRing, storageNodes, out success);
            if (nodeInfo == null)
                return null;
            return nodeInfo.Item2;
        }

        private static void HandleMngCtx(HttpListenerContext httpCtx)
        {
            var httpUrl = httpCtx.Request.Url;

            var managementRes = httpUrl.LocalPath;
            if (string.Equals(managementRes, "/logon", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleMngLogonRequest(httpCtx);
            }
            else if (string.Equals(managementRes, "/beginlogoff", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleMngBeginLogoff(httpCtx);
            }
            else if (string.Equals(managementRes, "/logoffcomplete", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleMngLogoffComplete(httpCtx);
            }
            else
            {
                httpCtx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                httpCtx.Response.Close(new byte[0], willBlock: false);
            }
        }

        private static void HandleMngLogonRequest(HttpListenerContext httpCtx)
        {
            var clientId = Guid.NewGuid();

            var value = new UriBuilder();
            value.Scheme = Uri.UriSchemeHttp;
            value.Host = httpCtx.Request.RemoteEndPoint.Address.ToString();
            value.Port = httpCtx.Request.RemoteEndPoint.Port;

            storageNodes[clientId] = value.Uri;
            nodeRing[FindNewRingId()] = clientId;

            var ringNodeUriDict = nodeRing.ToDictionary(nodeKvp => nodeKvp.Key, nodeKvp => new[] { nodeKvp.Value.ToString(), storageNodes[nodeKvp.Value].ToString() });

            httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
            httpCtx.Response.ContentType = MediaTypeNames.Application.Octet;
            httpCtx.Response.Close(clientId.ToByteArray(), willBlock: true);

            var redistributeClients = storageNodes.Where(kvp => kvp.Key != clientId).ToArray();

            var ringNodeSerializer = new DataContractJsonSerializer(ringNodeUriDict.GetType(),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

            byte[] ringNodeDataBytes;
            using (var ringNodeMemoryStream = new MemoryStream())
            {
                ringNodeSerializer.WriteObject(ringNodeMemoryStream, ringNodeUriDict);
                ringNodeDataBytes = ringNodeMemoryStream.ToArray();
            }

            foreach (var redistClient in redistributeClients)
            {
                var redistRequest = WebRequest.Create(redistClient.Value);
                redistRequest.Method = "REDISTRIBUTE";
                redistRequest.ContentType = "application/json";
                redistRequest.ContentLength = ringNodeDataBytes.LongLength;

                redistRequest.BeginGetRequestStream(ar =>
                {
                    var paramArray = ar.AsyncState as object[];
                    var req = paramArray[0] as WebRequest;
                    var dataBytes = paramArray[1] as byte[];

                    using (var reqStream = req.EndGetRequestStream(ar))
                        reqStream.Write(dataBytes, 0, dataBytes.Length);

                    using (var resp = req.GetResponse()) { }
                }, new object[] { redistRequest, ringNodeDataBytes });
            }
        }

        private static void HandleMngBeginLogoff(HttpListenerContext httpCtx)
        {
            var nodeId = Guid.Parse(httpCtx.Request.QueryString["nodeid"]);
            Uri nodeUri;
            if (storageNodes.TryRemove(nodeId, out nodeUri))
            {
                var logoffId = Guid.NewGuid();

                logoffsInProgress[logoffId] = nodeUri;

                httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
                httpCtx.Response.ContentType = MediaTypeNames.Application.Octet;
                httpCtx.Response.Close(logoffId.ToByteArray(), willBlock: false);
            }
            else
            {
                httpCtx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                httpCtx.Response.Close(new byte[0], willBlock: false);
            }
        }

        private static void HandleMngLogoffComplete(HttpListenerContext httpCtx)
        {
            var logoffId = Guid.Parse(httpCtx.Request.QueryString["logoffId"]);
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
            var nodeRingIds = nodeRing.Keys.ToArray();
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
