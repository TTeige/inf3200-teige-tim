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
                Console.WriteLine("Reading available nodes from file system.");

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

                    Console.WriteLine($"Adding available node {hostname} on port {port} to bag of available nodes.");
                    Controllers.NodesController.Nodes.Add(Tuple.Create(hostname, port));
                }
            }
            catch (FileNotFoundException)
            {
                Console.WriteLine("File with available node not found. Proceeding with empty nodes list.");
            }

            var nodesArray = Controllers.NodesController.Nodes.ToArray();
            Console.WriteLine($"Creating random network for {nodesArray.Length} available nodes.");
            var otherNodeBytes = new byte[nodesArray.Length > 0 ? nodesArray.Length - 1 : 0];
            var randomizer = new Random();
            for (int i = 0; i < nodesArray.Length; i++)
            {
                randomizer.NextBytes(otherNodeBytes);
                var otherConnectedNodes = new List<Tuple<string, int>>(otherNodeBytes.Length);
                int j = 0;
                foreach (var otherNodeByte in otherNodeBytes)
                {
                    if (j == i)
                        j++;

                    if (otherNodeByte > 64)
                        otherConnectedNodes.Add(nodesArray[j]);

                    j++;
                }

                Console.WriteLine($"Configuring node {nodesArray[i].Item1}:{nodesArray[i].Item2} to connect to the following nodes:");
                Console.WriteLine("\t", string.Join(Environment.NewLine + "\t", otherConnectedNodes.Select(nt => $"{nt.Item1}:{nt.Item2}")));
                string connectionList = string.Join(Environment.NewLine, otherConnectedNodes.Select(nt => $"{nt.Item1}:{nt.Item2}"));

                var nodeUriBuilder = new UriBuilder(Uri.UriSchemeHttp, nodesArray[i].Item1, nodesArray[i].Item2, "/connectToNodes");
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
