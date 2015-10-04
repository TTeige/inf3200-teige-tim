using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading;
using THNETII;

namespace UiT.Inf3200.StorageNodeServer
{
    static class StorageNodeServerProgram
    {
        private static ManualResetEvent terminateEvent = new ManualResetEvent(initialState: false);
        private static HttpListener httpListener = new HttpListener();
        private static Guid nodeGuid;

        private static ConcurrentDictionary<string, byte[]> kvps = new ConcurrentDictionary<string, byte[]>();
        private static Uri frontendUri;

        static void Main(string[] args)
        {
            AssemblySplash.WriteAssemblySplash();

            httpListener.Prefixes.Add("http://+:8181/");

            httpListener.Start();

            if (args != null && args.Length > 0)
            {
                frontendUri = new Uri(args[0]);

                Console.WriteLine("STORAGE : Logging on Storage node on Frontend ({0}) . . .", frontendUri);

                var logonRequest = WebRequest.Create(new Uri(frontendUri, "logon"));
                logonRequest.Method = "MANAGE";
                using (var logonResponse = logonRequest.GetResponse())
                {
                    Console.WriteLine("STORAGE : Storage node logged on Frontend ({0}), Assigned node GUID: {0}", frontendUri);
                    using (var memStream = new MemoryStream((int)logonResponse.ContentLength))
                    {
                        logonResponse.GetResponseStream().CopyTo(memStream);
                        nodeGuid = new Guid(memStream.ToArray());
                    }
                }

                Console.WriteLine("STORAGE : Assigned node GUID: {0}", nodeGuid);
            }

            httpListener.BeginGetContext(HandleHttpCtxCallback, 0U);

            Console.WriteLine("STORAGE : Server started and ready to accept requests . . .");

            terminateEvent.WaitOne();

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
                HandleKvpGet(httpCtx);
            }
            else if (string.Equals(httpMethod, WebRequestMethods.Http.Put, StringComparison.InvariantCultureIgnoreCase))
            {
                HandleKvpPut(httpCtx);
            }
            else if (string.Equals(httpMethod, "TERMINATE", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleTerminte(httpCtx);
                terminateEvent.Set();
            }
            else if (string.Equals(httpMethod, "SIZE", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleSize(httpCtx);
            }
            else if (string.Equals(httpMethod, "REDISTRIBUTE", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleRedistribute(httpCtx);
            }
            else if (string.Equals(httpMethod, "DIAG", StringComparison.InvariantCultureIgnoreCase))
            {
                HandleDiagnostics(httpCtx);
            }
            else
            {
                httpCtx.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
                httpCtx.Response.Close(new byte[0], willBlock: false);
            }
        }

        private static void HandleDiagnostics(HttpListenerContext httpCtx)
        {
            var serializer = new DataContractJsonSerializer(kvps.GetType(),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
            httpCtx.Response.ContentType = "application/json";
            using (var targetStream = httpCtx.Response.OutputStream)
                serializer.WriteObject(targetStream, kvps);
            httpCtx.Response.Close();
        }

        private static void HandleSize(HttpListenerContext httpCtx)
        {
            httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
            httpCtx.Response.ContentType = MediaTypeNames.Application.Octet;
            httpCtx.Response.Close(BitConverter.GetBytes(kvps.Count), willBlock: false);
        }

        private static void HandleRedistribute(HttpListenerContext httpCtx)
        {
            var serializer = new DataContractJsonSerializer(typeof(Dictionary<int, string[]>),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
            var nodeRingDict = serializer.ReadObject(httpCtx.Request.InputStream) as Dictionary<int, string[]>;
            if (nodeRingDict == null)
                return;

            var nodeRingGuidDict = nodeRingDict.ToDictionary(kvp => kvp.Key, kvp => Guid.Parse(kvp.Value[0]));
            var storageNodeDict = nodeRingDict.Values.ToDictionary(v => Guid.Parse(v[0]), v => new Uri(v[1]));

            var nodeRingKeys = nodeRingGuidDict.Keys.ToArray();
            var kvpArray = kvps.ToArray();
            foreach (var kvp in kvpArray)
            {
                bool storageNodeFound;
                Tuple<Guid, Uri> targetNodeInfo;
                targetNodeInfo = StorageNodeFinder.FindStorageNode(nodeRingKeys,
                    kvp.Key.GetHashCode(), nodeRingGuidDict, storageNodeDict, out storageNodeFound);
                if (!storageNodeFound)
                    continue;

                if (targetNodeInfo.Item1 != nodeGuid)
                {
                    var otherNodePutRequest = WebRequest.Create(new Uri(targetNodeInfo.Item2, kvp.Key));
                    otherNodePutRequest.Method = WebRequestMethods.Http.Put;
                    otherNodePutRequest.ContentLength = kvp.Value.LongLength;
                    otherNodePutRequest.BeginGetRequestStream(ar =>
                    {
                        var paramArray = ar.AsyncState as object[];
                        var dataBytes = paramArray[0] as byte[];
                        var req = paramArray[1] as WebRequest;

                        using (var reqStream = req.EndGetRequestStream(ar))
                        {
                            reqStream.Write(dataBytes, 0, dataBytes.Length);
                            reqStream.Flush();
                        }
                        using (var resp = req.GetResponse()) { }
                    }, new object[] { kvp.Value, otherNodePutRequest });

                    byte[] ignoreValue;
                    kvps.TryRemove(kvp.Key, out ignoreValue);
                }
            }

            httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
            httpCtx.Response.Close(new byte[0], willBlock: false);
        }

        private static void HandleTerminte(HttpListenerContext httpCtx)
        {
            if (frontendUri == null)
            {
                httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
                httpCtx.Response.Close(new byte[0], willBlock: true);
                httpListener.Stop();
                return;
            }

            var beginLogoffRequest = WebRequest.Create(new Uri(frontendUri, "beginlogoff?nodeid=" + Uri.EscapeUriString(nodeGuid.ToString())));
            beginLogoffRequest.Method = "MANAGE";
            Guid logoffGuid;
            using (var beginLogoffResponse = beginLogoffRequest.GetResponse())
            {
                using (var memStream = new MemoryStream((int)beginLogoffResponse.ContentLength))
                {
                    beginLogoffResponse.GetResponseStream().CopyTo(memStream);
                    logoffGuid = new Guid(memStream.ToArray());
                }
            }

            httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
            httpCtx.Response.Close(new byte[0], willBlock: true);
            httpListener.Stop();

            var frontendClient = new WebClient() { BaseAddress = frontendUri.ToString() };

            while (!kvps.IsEmpty)
            {
                var key = kvps.Keys.First();
                byte[] value;
                if (kvps.TryRemove(key, out value))
                {
                    frontendClient.UploadData(key, WebRequestMethods.Http.Put, value);
                }
            }

            var LogoffCompleteRequest = WebRequest.Create(new Uri(frontendUri, "logoffcomplete?logoffId=" + Uri.EscapeUriString(logoffGuid.ToString())));
            beginLogoffRequest.Method = "MANAGE";
            using (var logoffCompleteReponse = LogoffCompleteRequest.GetResponse()) { }
        }

        private static void HandleKvpGet(HttpListenerContext httpCtx)
        {
            var key = httpCtx.Request.Url.LocalPath;

            httpCtx.Response.ContentType = "application/octet-stream";

            byte[] value;
            if (kvps.TryGetValue(key, out value))
                httpCtx.Response.Close(value, willBlock: false);
            else
            {
                httpCtx.Response.StatusCode = (int)HttpStatusCode.NotFound;
                httpCtx.Response.Close(new byte[0], willBlock: true);
            }
        }

        private static void HandleKvpPut(HttpListenerContext httpCtx)
        {
            var key = httpCtx.Request.Url.LocalPath;
            byte[] value;
            using (var memStream = new MemoryStream((int)httpCtx.Request.ContentLength64))
            {
                httpCtx.Request.InputStream.CopyTo(memStream);
                value = memStream.ToArray();
            }

            kvps[key] = value;

            httpCtx.Response.StatusCode = (int)HttpStatusCode.OK;
            httpCtx.Response.Close(new byte[0], willBlock: true);
        }
    }
}
