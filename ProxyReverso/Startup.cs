using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Service.Proxy;

namespace ProxyReverso
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddHttpProxy();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHttpProxy httpProxy)
        {


            var httpClient = new HttpMessageInvoker(new SocketsHttpHandler()
            {
                UseProxy = false,
                AllowAutoRedirect = false,
                AutomaticDecompression = System.Net.DecompressionMethods.None,
                UseCookies = false
            });

            var transformer = new CustomTransformer();
            var requestOptions = new RequestProxyOptions();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.Map("/{**catch-all}", async httpContext =>
                {
                    var urls = getUrls();
                    var teste = httpContext.Request.Headers.FirstOrDefault(c => c.Key == "teste").Value;
                    var url = urls.FirstOrDefault(x => x.key == "1").url;
                    if (teste.Count != 0)
                        url = urls?.FirstOrDefault(x => x.key == teste.ToString()).url;

                    await httpProxy.ProxyAsync(httpContext, url, httpClient, requestOptions);
                    var errorFeature = httpContext.GetProxyErrorFeature();
                    if (errorFeature != null)
                    {
                        var error = errorFeature.Error;
                        var exception = errorFeature.Exception;
                    }

                });
            });

        }


        private List<objetoRetorno> getUrls()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://localhost:5001/weatherforecast/url");
            using HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            using Stream stream = response.GetResponseStream();
            using StreamReader reader = new StreamReader(stream);

            var returno = reader.ReadToEnd();
            return JsonConvert.DeserializeObject<List<objetoRetorno>>(returno);

        }
        private class CustomTransformer : HttpTransformer
        {
            /// <summary>
            /// A callback that is invoked prior to sending the proxied request. All HttpRequestMessage
            /// fields are initialized except RequestUri, which will be initialized after the
            /// callback if no value is provided. The string parameter represents the destination
            /// URI prefix that should be used when constructing the RequestUri. The headers
            /// are copied by the base implementation, excluding some protocol headers like HTTP/2
            /// pseudo headers (":authority").
            /// </summary>
            /// <param name="httpContext">The incoming request.</param>
            /// <param name="proxyRequest">The outgoing proxy request.</param>
            /// <param name="destinationPrefix">The uri prefix for the selected destination server which can be used to create
            /// the RequestUri.</param>
            public override async ValueTask TransformRequestAsync(HttpContext httpContext, HttpRequestMessage proxyRequest, string destinationPrefix)
            {
                // Copy all request headers
                await base.TransformRequestAsync(httpContext, proxyRequest, destinationPrefix);

                // Customize the query string:
                //var queryContext = new QueryTransformContext(httpContext.Request);
                //queryContext.Collection.Remove("param1");
                //queryContext.Collection["area"] = "xx2";

                // Assign the custom uri. Be careful about extra slashes when concatenating here.
                // proxyRequest.RequestUri = new Uri(destinationPrefix + httpContext.Request.Path + queryContext.QueryString);

                // Suppress the original request header, use the one from the destination Uri.
                proxyRequest.Headers.Host = null;
            }

        }
    }
}