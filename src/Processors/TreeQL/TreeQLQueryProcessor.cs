using Alloy.Liquid.Models.Pages;
using DeaneBarker.Data.Features.Catalog.Querying;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;

namespace Alloy.Liquid.Endpoints.Processors.TreeQL
{
    public class TreeQlQueryProcessor : IQueryProcessor
    {
        private IContentLoader _loader = ServiceLocator.Current.GetInstance<IContentLoader>();

        // This can be overriden with install-specific logic
        public static Func<IContent, string> ContentLabelProvider = GetContentLabel;

        public object GetData(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var query = TreeQueryParser.Parse(input);

            var pages = query.Scope switch
            {
                "children" => GetChildren(query.Target.Path),
                "descendants" => GetDescendants(query.Target.Path),
                _ => GetSelf(query.Target.Path)
            };

            if (query.Sort.Any())
            {
                var sortString = $"{query.Sort.FirstOrDefault().Value}:{query.Sort.FirstOrDefault().Direction}";
                pages = sortString switch
                {
                    "date:Ascending" => pages.Where(p => p is IVersionable).OrderBy(p => ((IVersionable)p).StartPublish),
                    "date:Descending" => pages.Where(p => p is IVersionable).OrderByDescending(p => ((IVersionable)p).StartPublish),
                    "name:Ascending" => pages.OrderBy(p => ((IContent)p).Name),
                    "name:Descending" => pages.OrderByDescending(p => ((IContent)p).Name),
                    _ => pages
                };
            }

            if (query.Skip != 0)
            {
                pages = pages.Skip(query.Skip);
            }

            if (query.Limit != 0)
            {
                pages = pages.Take((int)query.Limit);
            }

            return pages;
        }

        public IEnumerable<string> GetParseErrors(string query)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<ContentData> GetChildren(string path)
        {
            var parent = GetReferenceFromTarget(path);
            return _loader.GetChildren<ContentData>(parent);
        }

        public IEnumerable<ContentData> GetDescendants(string path)
        {
            var parent = GetReferenceFromTarget(path);
            return _loader.GetDescendents(parent).Select(p => _loader.Get<ContentData>(p));
        }

        public IEnumerable<ContentData> GetSelf(string path)
        {
            return new List<ContentData>() { _loader.Get<ContentData>(GetReferenceFromTarget(path)) };
        }

        private ContentReference GetReferenceFromTarget(string path)
        {
            // Content label
            if (path.StartsWith("label:"))
            {
                // Broken out for debugging
                path = path.Split(":").Last();
                var pages = _loader.GetDescendents(ContentReference.StartPage)
                    .Select(p => _loader.Get<PageData>(p));
                var pagesWithLabel = pages.Where(p => ContentLabelProvider(p) != null);
                var pagesWithThisLabel = pagesWithLabel.Where(p => ContentLabelProvider(p) == path.ToLower().Trim());

                return pagesWithThisLabel.FirstOrDefault()?.ContentLink;
            };

            // URL path
            var builder = new UrlBuilder(path);
            return UrlResolver.Current.Route(builder, EPiServer.Web.ContextMode.Default)?.ContentLink;
        }

        // The default implementation
        private static string GetContentLabel(IContent c)
        {
            var contentLabelPropertyName = "ContentLabel";
            return c.Property[contentLabelPropertyName]?.Value?.ToString().ToLower().Trim();
        }
    }
}
