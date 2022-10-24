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
        private IQueryProcessor _processor = new TreeQlQueryProcessor();

        public ActionResult Index(EndpointPage endpoint)
        {
            var query = endpoint.Query;
            var liquid = endpoint.Template;

            // So, right now we return a collection of items
            // But, theoretically, we don't have to
            // Leaving this at just an object for now. Might change it later
            var model = (IEnumerable<ContentData>)_processor.GetData(query);

            if (string.IsNullOrWhiteSpace(liquid))
            {
                return Json(model.Select(c => getSimplifiedObject((IContent)c)));

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

            TemplateOptions.Default.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
            var parser = new CmsFluidViewParser(new FluidParserOptions() { AllowFunctions = true });

            // It should be guaranteed of parsing because it passed validation to get to this point.
            var template = parser.Parse(liquid);

            // See notes below. Context models are weird ojn MVC installs
            var context = new TemplateContext(new SebIsWrong() { Model = model });
            context.Options.Filters.WithUrlFilters();
            context.SetValue("ContentLoader", new ContentLoaderValue());

            var result = string.Empty;
            var statusCode = 200;

            try
            {
                result = template.Render(context);
            }
            catch(Exception e)
            {
                result = "Error: " + e.Message;
                statusCode = 500;
            }

            return new ContentResult()
            {
                Content = result,
                StatusCode = statusCode,
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
