using DeaneBarker.Optimizely.Endpoints.Models;
using DeaneBarker.Optimizely.Endpoints.TreeQL;
using EPiServer.Web.Mvc;
using Fluid;
using Microsoft.AspNetCore.Mvc;
using Optimizely.CMS.Labs.LiquidTemplating.Filters;
using Optimizely.CMS.Labs.LiquidTemplating.Values;
using Optimizely.CMS.Labs.LiquidTemplating.ViewEngine;
using System.Diagnostics;

namespace DeaneBarker.Optimizely.Endpoints.Controllers
{
    public class EndpointController : PageController<EndpointPage>
    {
        public string Index(IContent currentPage)
        {
            Stopwatch sw = new();

            var endpoint = currentPage as IEndpoint;
            if(endpoint == null)
            {
                // No idea how this might happen...
                throw new ArgumentException("Current page is not IEndpoint");
            }

            var query = endpoint.QuerySource;
            var template = endpoint.TemplateSource;

            // So, right now we return a collection of items
            // But, theoretically, we don't have to
            // Leaving this at just an object for now. Might change it 
            sw.Restart();
            var model = endpoint.QueryProcessor.GetData(query, (IContent)endpoint); // WTF? Why do I pass this into itself?
            var queryTime = sw.ElapsedMilliseconds;

            var result = string.Empty;
            var statusCode = 200;

            sw.Restart();
            try
            {
                result = endpoint.Transformer.Transform(template, model);
            }
            catch(Exception e)
            {
                result = $"TRANSFORMATION ERROR: {e.Message}" + Environment.NewLine + e.StackTrace;
                statusCode = 500;
            }
            var transformTime = sw.ElapsedMilliseconds;

            Response.StatusCode = statusCode;
            Response.ContentType = GetContentType(result);

            Response.Headers.Add("x-query-time", $"{queryTime}ms");
            Response.Headers.Add("x-transform-time", $"{transformTime}ms");

            return result;
        }


        private string GetContentType(string source)
        {
            // This is probably bad...
            source = source.Trim();

            // Okay, this SHOULD work, but out of nowhere, the server started returning 406 errors whenever the content was anything other than "text/plan"
            // I need to investigate this

            //if (source.StartsWith("<"))
            //{
            //    return "text/html";
            //}

            //if (source.StartsWith("[") || source.StartsWith("{"))
            //{
            //    return "application/json";
            //}

            return "text/plain";
        }
    }
}
