﻿using EPiServer.Cms.Shell;
using EPiServer.ServiceLocation;
using EPiServer.Web.Routing;
using System.Reflection.Metadata.Ecma335;

namespace DeaneBarker.Optimizely
{
    public class ContentValueProvider : IContentValueProvider
    {
        protected Dictionary<string, Func<IContent, string, object>> map = new();

        protected IContentLoader _loader = ServiceLocator.Current.GetInstance<IContentLoader>();
        protected IContentTypeRepository _typeRepo = ServiceLocator.Current.GetInstance<IContentTypeRepository>();

        public ContentValueProvider()
        {
            // This maps field names in TreeQL WHERE clauses to functions that return the values

            map.Add("name", GetName);
            map.Add("title", GetName);

            map.Add("published", GetStartPublish);
            map.Add("date", GetStartPublish);
            map.Add("startpublish", GetStartPublish);
            map.Add("published_month", GetStartPublish);
            map.Add("published_year", GetStartPublish);

            map.Add("language", GetLanguage);

            map.Add("type", GetType);

            map.Add("path_segments", GetSegments);

            map.Add("depth", GetDepth);

            map.Add("ordinal", GetSortIndex);
            map.Add("sort_index", GetSortIndex);
            map.Add("order", GetSortIndex);

            map.Add("url", GetUrl);
        }

        public object GetValue(IContent content, string field)
        {
            if (map.ContainsKey(field))
            {
                return map[field](content, field);
            }

            return GetStringValue(content, field);
        }

        protected object GetName(IContent content, string field)
        {
            return content.Name;
        }

        protected object GetStringValue(IContent content, string field)
        {
            return content.Property[field]?.Value?.ToString();
        }

        protected object GetStartPublish(IContent content, string field)
        {
            var publishedDate = ((IVersionable)content).StartPublish;
            if (!publishedDate.HasValue)
            {
                return null;
            }

            if (field == "published_month")
            {
                return publishedDate.Value.Month;
            }

            if (field == "published_year")
            {
                return publishedDate.Value.Year;
            }

            return publishedDate.Value;
        }

        protected object GetUrl(IContent content, string field)
        {
            return UrlResolver.Current.GetUrl(content.ContentLink);
        }

        protected object GetLanguage(IContent content, string field)
        {
            return content.LanguageBranch();
        }

        protected object GetSegments(IContent content, string field)
        {
            var path = UrlResolver.Current.GetUrl(content);
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            return path.Split('/', StringSplitOptions.RemoveEmptyEntries);
        }

        protected object GetDepth(IContent content, string field)
        {
            return _loader.GetAncestors(content.ContentLink).Count();
        }

        protected object GetSortIndex(IContent content, string field)
        {
            if (!(content is PageData))
            {
                return 0;
            }

            return ((PageData)content).SortIndex;
        }

        protected object GetType(IContent content, string field)
        {
            return _typeRepo.Load(content.ContentTypeID).Name;
        }
    }
}