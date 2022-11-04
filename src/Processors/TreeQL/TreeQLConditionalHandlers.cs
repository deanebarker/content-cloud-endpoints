using Castle.DynamicProxy.Generators.Emitters.SimpleAST;
using EPiServer.ServiceLocation;
using System.Diagnostics.Eventing.Reader;
using DeaneBarker.TreeQL;

namespace DeaneBarker.Optimizely.Endpoints.TreeQL
{
    public class TreeQLConditionalHandlers
    {
        public static Dictionary<string, Func<Filter, IContent, bool>> Map = new();

        private static IContentValueProvider _contentValueProvider = ServiceLocator.Current.GetInstance<IContentValueProvider>();

        static TreeQLConditionalHandlers()
        {
            Map.Add("=", Equals);
            Map.Add("is", Equals);

            Map.Add(">", GreaterThan);
            Map.Add(">=", GreaterThan);

            Map.Add("<", LessThan);
            Map.Add("<=", LessThan);

            Map.Add("!=", NotEqualTo);
            Map.Add("<>", NotEqualTo);

            Map.Add("contains", Contains);
            
            Map.Add("startswith", StartsWith);
            Map.Add("starts", StartsWith);
            
            Map.Add("endswith", EndsWith);
            Map.Add("ends", EndsWith);
        }

        public static bool Evaluate(Filter filter, IContent content)
        {
            if(Map.ContainsKey(filter.Operator))
            {
                return Map[filter.Operator](filter, content);
            }

            return Equals(filter, content);
        }

        private static bool Equals(Filter filter, IContent content)
        {
            return _contentValueProvider.GetValue(content, filter.FieldName).ToString().ToLower().Trim() == filter.Value.ToLower().Trim();
        }
        private static bool NotEqualTo(Filter filter, IContent content)
        {
            return _contentValueProvider.GetValue(content, filter.FieldName).ToString() != filter.Value;
        }

        private static bool GreaterThan(Filter filter, IContent content)
        {
            var contentValue = _contentValueProvider.GetValue(content, filter.FieldName);

            if(contentValue is DateTime)
            {
               if(!DateTime.TryParse(filter.Value, out DateTime filterValue))
               {
                    return false;
               }
                return filter.Operator switch
                {
                    ">" => (DateTime)contentValue > filterValue,
                    ">=" => (DateTime)contentValue >= filterValue,
                    _ => false
                };
            }

            if (contentValue is int)
            {
                if (!int.TryParse(filter.Value, out int filterValue))
                {
                    return false;
                }
                return filter.Operator switch
                {
                    "<" => (int)contentValue > filterValue,
                    "<=" => (int)contentValue >= filterValue,
                    _ => false
                };
            }

            // How do eval "greater than" between two strings?
            return false;
        }

        private static bool LessThan(Filter filter, IContent content)
        {
            var contentValue = _contentValueProvider.GetValue(content, filter.FieldName);

            if (contentValue is DateTime)
            {
                if (!DateTime.TryParse(filter.Value, out DateTime filterValue))
                {
                    return false;
                }
                return filter.Operator switch
                {
                    "<" => (DateTime)contentValue < filterValue,
                    "<=" => (DateTime)contentValue <= filterValue,
                    _ => false
                };
            }

            if (contentValue is int)
            {
                if (!int.TryParse(filter.Value, out int filterValue))
                {
                    return false;
                }
                return filter.Operator switch
                {
                    "<" => (int)contentValue < filterValue,
                    "<=" => (int)contentValue <= filterValue,
                    _ => false
                };
            }

            // How do eval "greater than" between two strings?
            return false;
        }

        private static bool Contains(Filter filter, IContent content)
        {
            var contentValue = _contentValueProvider.GetValue(content, filter.FieldName);

            // "contains" only works on strings
            if(contentValue is string)
            {
                return contentValue.ToString().ToLower().Contains(filter.Value.ToLower());
            }

            if(contentValue is IEnumerable<string>)
            {
                return ((IEnumerable<string>)contentValue).Contains(filter.Value);
            }

            if (contentValue is IEnumerable<int>)
            {
                return ((IEnumerable<int>)contentValue).ToArray().Contains(int.Parse(filter.Value));
            }

            return false;
        }

        private static bool StartsWith(Filter filter, IContent content)
        {
            var contentValue = _contentValueProvider.GetValue(content, filter.FieldName) as string;
            if(contentValue == null)
            {
                return false;
            }

            return contentValue.StartsWith(filter.Value.ToLower());
        }

        private static bool EndsWith(Filter filter, IContent content)
        {
            var contentValue = _contentValueProvider.GetValue(content, filter.FieldName) as string;
            if (contentValue == null)
            {
                return false;
            }

            return contentValue.EndsWith(filter.Value.ToLower());
        }
    }
}
