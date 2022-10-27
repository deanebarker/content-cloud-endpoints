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
        public ActionResult Index(IContent currentPage)
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

            if (string.IsNullOrWhiteSpace(template))
            {
                return Json(((IEnumerable<ContentData>)model).Select(c => getSimplifiedObject((IContent)c)));

                // This needs to get replaced with the RIGHT way to do this
                // 2022-10-21: email into Aniel and Magnus
                object getSimplifiedObject(IContent content)
                {
                    return new
                    {
                        name = content.Name,
                        contentLink = content.ContentLink
                    };
                }

            }

            var result = endpoint.Transformer.Transform(template, model);

            return new ContentResult()
            {
                Content = result,
                StatusCode = 200,
                ContentType = "text/html"
            };
        }
    }

    // This is the result of a long conversation I had with Sebastian.
    // I still don't quite understand it, but models are weird with MVC installs
    public class SebIsWrong
    {
        public object Model { get; set; }
    }
}
