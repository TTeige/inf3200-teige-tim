using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.Dnx.Runtime;
using Microsoft.Framework.Configuration;
using Microsoft.Framework.DependencyInjection;
using Microsoft.Framework.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using UiT.Inf3200.FrontendApp.Models;

namespace UiT.Inf3200.FrontendApp
{
    public class Startup
    {
        public Startup(IHostingEnvironment env, IApplicationEnvironment appEnv)
        {
            HostingEnvironment = env;
            ApplicationEnvironment = appEnv;

            // Setup configuration sources.
            var builder = new ConfigurationBuilder()
                .SetBasePath(appEnv.ApplicationBasePath)
                .AddJsonFile("appsettings.json")
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            try
            {
                var fileLines = File.ReadAllLines(Path.Combine(appEnv.ApplicationBasePath, "availableNodes"));

                foreach (var fileLine in fileLines)
                {
                    string hostname;
                    int port = 8899;
                    int colIdx = fileLine.LastIndexOf(':');
                    if (colIdx < 0)
                        hostname = fileLine.Trim();
                    else
                    {
                        hostname = fileLine.Substring(0, colIdx).Trim();
                        port = int.Parse(fileLine.Substring(colIdx + 1).Trim());
                    }

                    var uriBuilder = new UriBuilder(Uri.UriSchemeHttp, hostname, port, "GetGuid");
                    var req = WebRequest.Create(uriBuilder.Uri) as HttpWebRequest;
                    try
                    {
                        Guid guid;
                        using (var resp = req.GetResponse() as HttpWebResponse)
                        {
                            var respEncoding = string.IsNullOrWhiteSpace(resp.ContentEncoding) ? Encoding.ASCII : (Encoding.GetEncoding(resp.ContentEncoding) ?? Encoding.ASCII);
                            using (var respReader = new StreamReader(resp.GetResponseStream()))
                            {
                                guid = Guid.Parse(respReader.ReadLine());
                            }
                        }

                        Controllers.NodesController.Nodes[guid] = Tuple.Create(hostname, port);
                    }
                    catch (WebException) { Console.WriteLine($"Unable to reach node on host {hostname} at port {port}."); }
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File with available node not found. Proceeding with empty nodes list.");
            }

            var nodesArray = Controllers.NodesController.Nodes.ToArray().Select(kvp => new NodeModel { Guid = kvp.Key, Hostname = kvp.Value.Item1, Port = kvp.Value.Item2 }).ToArray();
            var otherNodeBytes = new byte[nodesArray.Length > 0 ? nodesArray.Length - 1 : 0];
            var randomizer = new Random();
            for (int i = 0; i < nodesArray.Length; i++)
            {
                randomizer.NextBytes(otherNodeBytes);
                var otherConnectedNodes = new List<NodeModel>(otherNodeBytes.Length);
                int j = 0;
                foreach (var otherNodeByte in otherNodeBytes)
                {
                    if (j == i)
                        j++;

                    if (otherNodeByte > (128 + 64))
                        otherConnectedNodes.Add(nodesArray[j]);

                    j++;
                }

                string connectionList = string.Join(Environment.NewLine, otherConnectedNodes.Select(nt => $"{nt.Hostname}:{nt.Port}"));

                var nodeUriBuilder = new UriBuilder(Uri.UriSchemeHttp, nodesArray[i].Hostname, nodesArray[i].Port, "/connectToNodes");
                var req = WebRequest.Create(nodeUriBuilder.Uri) as HttpWebRequest;
                req.Method = WebRequestMethods.Http.Post;
                req.ContentType = new ContentType(MediaTypeNames.Text.Plain) { CharSet = Encoding.ASCII.WebName }.ToString();
                var reqData = Encoding.ASCII.GetBytes(connectionList);
                req.ContentLength = reqData.LongLength;

                try
                {
                    using (var reqStream = req.GetRequestStream())
                    {
                        reqStream.Write(reqData, 0, reqData.Length);
                        reqStream.Flush();
                    }
                    using (var resp = req.GetResponse()) { }
                }
                catch (WebException) { }
            }
        }

        public IHostingEnvironment HostingEnvironment { get; }

        public IApplicationEnvironment ApplicationEnvironment { get; }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();

            // Uncomment the following line to add Web API services which makes it easier to port Web API 2 controllers.
            // You will also need to add the Microsoft.AspNet.Mvc.WebApiCompatShim package to the 'dependencies' section of project.json.
            // services.AddWebApiConventions();


        }

        // Configure is called after ConfigureServices is called.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.MinimumLevel = LogLevel.Information;
            loggerFactory.AddConsole();
            loggerFactory.AddDebug();

            // Configure the HTTP request pipeline.

            // Add the platform handler to the request pipeline.
            app.UseIISPlatformHandler();

            // Add the following to the request pipeline only in development environment.
            app.UseDeveloperExceptionPage();

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Add MVC to the request pipeline.
            app.UseMvcWithDefaultRoute();

            app.UseWelcomePage("/Welcome");

            app.UseRuntimeInfoPage("/RuntimeInfo");


        }
    }
}
