﻿using Castle.Components.DictionaryAdapter;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Runtime.Intrinsics.X86;

namespace DeaneBarker.Optimizely.Endpoints.TreeQL
{
    public class TreeQlQueryProcessor : IQueryProcessor
    {
        private IContentLoader _loader = ServiceLocator.Current.GetInstance<IContentLoader>();
        private static IContentValueProvider _contentValueProvider = ServiceLocator.Current.GetInstance<IContentValueProvider>();

        // This can be overriden with install-specific logic
        public static Func<IContent, string> ContentLabelProvider = GetContentLabel;

        public object GetData(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            var query = TreeQueryParser.Parse(input);

            // Get the base set of content to query
            var content = query.Scope switch
            {
                "children" => GetChildren(query.Target.Path).AsQueryable(),
                "descendants" => GetDescendants(query.Target.Path).AsQueryable(),
                _ => GetSelf(query.Target.Path).AsQueryable()
            };

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
                    content = content.OrderBy(c => _contentValueProvider.GetValue((IContent)c, sort.Value));
                }
                else
                {
                    content = content.OrderByDescending(c => _contentValueProvider.GetValue((IContent)c, sort.Value));
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
}
