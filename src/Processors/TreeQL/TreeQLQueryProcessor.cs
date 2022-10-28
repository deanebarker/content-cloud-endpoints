using EPiServer.ServiceLocation;
using EPiServer.Validation;
using EPiServer.Web.Routing;
using Fluid;
using System.Linq.Expressions;

namespace DeaneBarker.Optimizely.Endpoints.TreeQL
{
    public class TreeQlQueryProcessor : IQueryProcessor
    {
        private IContentLoader _loader = ServiceLocator.Current.GetInstance<IContentLoader>();
        private static IContentValueProvider _contentValueProvider = ServiceLocator.Current.GetInstance<IContentValueProvider>();

        // This can be overriden with install-specific logic
        public static Func<IContent, string> ContentLabelProvider = GetContentLabel;

        public object GetData(string input, IContent endpoint)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var query = TreeQueryParser.Parse(input);

            // Get the base set of content to query
            var content = GetContentFromQuery(query, endpoint).AsQueryable();

            // Single filters are easy
            if(query.Filters.Count == 1)
            {
                content = content.Where(c => TreeQLConditionalHandlers.Evaluate(query.Filters.First(), (IContent)c));
            }

            // Multiple filters have to use PredicateBuilder because "OR" conjunctions are painful...
            // I could just use this for OR conjunctions, but for consistency, I use it for both
            if (query.Filters.Count > 1)
            {
                var conjunction = query.Filters[1].Conjunction ?? "and"; // This should never be NULL...

                if (conjunction == "and")
                {
                    var andPredicate = PredicateBuilder.True<ContentData>();
                    foreach (var andFilter in query.Filters)
                    {
                        andPredicate = andPredicate.And(c => TreeQLConditionalHandlers.Evaluate(andFilter, (IContent)c));
                    }
                    content = content.Where(andPredicate);
                }

                if (conjunction == "or")
                {
                    var orPredicate = PredicateBuilder.False<ContentData>();
                    foreach (var orFilter in query.Filters)
                    {
                        orPredicate = orPredicate.Or(c => TreeQLConditionalHandlers.Evaluate(orFilter, (IContent)c));
                    }
                    content = content.Where(orPredicate);
                }
            }
            
            // Sort it
            foreach (var sort in query.Sort)
            {
                if (sort.Direction == SortDirection.Ascending)
                {
                    content = content.AppendOrderBy(c => _contentValueProvider.GetValue((IContent)c, sort.Value));
                }
                else
                {
                    content = content.AppendOrderByDescending(c => _contentValueProvider.GetValue((IContent)c, sort.Value));
                }
            }
            
            // Skip it
            content = content.Skip(query.Skip);

            // Limit it
            if (query.Limit != 0)
            {
                content = content.Take((int)query.Limit);
            }

            return content;
        }

        public IEnumerable<ContentData> GetContentFromQuery(TreeQuery query, IContent endpoint)
        {
            return query switch
            {
                { Scope: "children" } => GetChildren(query.Target.Path),
                { Scope: "descendants" } => GetDescendants(query.Target.Path),
                { Scope: "parent" } => GetParent(query.Target.Path),
                { Scope: "ancestors" } => GetAncestors(query.Target.Path),
                { Scope: "siblings" } => GetSiblings(query.Target.Path),
                { Scope: "results", Target.Path: "@parent" } => GetParentResults(query.Target.Path, endpoint),
                _ => GetSelf(query.Target.Path)
            };
        }

        public IEnumerable<ValidationError> GetParseErrors(string query)
        {
            try
            {
                _ = TreeQueryParser.Parse(query); // We don't actually care what comes back, we just want to see if we get an exception...
            }
            catch (Exception e)
            {
                return new List<ValidationError>()
                {
                    new ValidationError()
                    {
                        ErrorMessage = e.Message,
                        PropertyName = "Query",
                        Severity = ValidationErrorSeverity.Error,
                        ValidationType = ValidationErrorType.PropertyValidation
                    }
                };
            }

            return null;
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

        public IEnumerable<ContentData> GetParent(string path)
        {
            var item = GetReferenceFromTarget(path);
            return new List<ContentData>() { _loader.Get<ContentData>(_loader.Get<IContent>(item).ParentLink) };
        }

        public IEnumerable<ContentData> GetAncestors(string path)
        {
            var item = GetReferenceFromTarget(path);
            return _loader.GetAncestors(item).Select(i => _loader.Get<ContentData>(i.ContentLink));
        }

        public IEnumerable<ContentData> GetSiblings(string path)
        {
            var item = GetReferenceFromTarget(path);
            var parent = _loader.Get<IContent>(item).ParentLink;
            return _loader.GetChildren<ContentData>(parent);
        }

        public IEnumerable<ContentData> GetParentResults(string path, IContent endpoint)
        {
            var parent = _loader.Get<IContent>(endpoint.ParentLink);
            var source = ((IEndpoint)parent).QuerySource;
            var query = TreeQueryParser.Parse(source);

            return GetContentFromQuery(query, parent);
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

            // Content ID
            if(path.All(c => char.IsDigit(c)))
            {
                return new ContentReference(int.Parse(path));
            }

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

    // From: http://www.albahari.com/nutshell/predicatebuilder.aspx
    public static class PredicateBuilder
    {
        public static Expression<Func<T, bool>> True<T>() { return f => true; }
        public static Expression<Func<T, bool>> False<T>() { return f => false; }

        public static Expression<Func<T, bool>> Or<T>(this Expression<Func<T, bool>> expr1,
                                                            Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>
                  (Expression.OrElse(expr1.Body, invokedExpr), expr1.Parameters);
        }

        public static Expression<Func<T, bool>> And<T>(this Expression<Func<T, bool>> expr1,
                                                             Expression<Func<T, bool>> expr2)
        {
            var invokedExpr = Expression.Invoke(expr2, expr1.Parameters.Cast<Expression>());
            return Expression.Lambda<Func<T, bool>>
                  (Expression.AndAlso(expr1.Body, invokedExpr), expr1.Parameters);
        }
    }

    // From: https://stackoverflow.com/questions/13497255/programmatically-chain-orderby-thenby-using-linq-entity-framework/45486019#45486019
    public static class QueryableExtensions
    {
        public static IOrderedQueryable<T> AppendOrderBy<T, TKey>(this IQueryable<T> query, Expression<Func<T, TKey>> keySelector)
            => query.Expression.Type == typeof(IOrderedQueryable<T>)
            ? ((IOrderedQueryable<T>)query).ThenBy(keySelector)
            : query.OrderBy(keySelector);

        public static IOrderedQueryable<T> AppendOrderByDescending<T, TKey>(this IQueryable<T> query, Expression<Func<T, TKey>> keySelector)
            => query.Expression.Type == typeof(IOrderedQueryable<T>)
                ? ((IOrderedQueryable<T>)query).ThenByDescending(keySelector)
                : query.OrderByDescending(keySelector);
    }
}
