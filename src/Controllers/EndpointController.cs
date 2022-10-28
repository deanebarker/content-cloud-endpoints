using DeaneBarker.Optimizely.Endpoints.Models;
using DeaneBarker.Optimizely.Endpoints.TreeQL;
using EPiServer.Web.Mvc;
using Fluid;
using Microsoft.AspNetCore.Mvc;
using Optimizely.CMS.Labs.LiquidTemplating.Filters;
using Optimizely.CMS.Labs.LiquidTemplating.Values;
using Optimizely.CMS.Labs.LiquidTemplating.ViewEngine;

namespace DeaneBarker.Optimizely.Endpoints.Controllers
{
    public class EndpointController : PageController<EndpointPage>
    {
        public string Index(IContent currentPage)
        {
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
            // Leaving this at just an object for now. Might change it later
            var model = endpoint.QueryProcessor.GetData(query, (IContent)endpoint); // WTF? Why do I pass this into itself?

            var result = endpoint.Transformer.Transform(template, model);

            Response.StatusCode = 200;
            Response.ContentType = GetContentType(result);

            return result;
        }


        private string GetContentType(string source)
        {
            // This is probably bad...
            source = source.Trim();

            // Okay, this SHOULD work, but out of nowhere, the server started returning 406 errors whenever the content was anything other than "text/plan"
            // I need to investigate this

            //if(source.StartsWith("<"))
            //{
            //    return "text/html";
            //}

            //if(source.StartsWith("[") || source.StartsWith("{"))
            //{
            //    return "application/json";
            //}

            return "text/plain";
        }
    }

    // This is the result of a long conversation I had with Sebastian.
    // I still don't quite understand it, but models are weird with MVC installs
    public class SebIsWrong
    {
        public object Model { get; set; }
    }
}
