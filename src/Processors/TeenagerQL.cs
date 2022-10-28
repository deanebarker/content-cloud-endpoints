using EPiServer.ServiceLocation;
using EPiServer.Validation;
using EPiServer.Web.Routing;
using System.Text.RegularExpressions;

namespace DeaneBarker.Optimizely.Endpoints.Processoers
{
    public class TeenagerQL : IQueryProcessor
    {
        private string emphasisWord = "really";
        private IContentLoader _loader = ServiceLocator.Current.GetInstance<IContentLoader>();

        public object GetData(string query, IContent content)
        {
            var howBadDoYouWantContent = Regex.Matches(query.ToLower(), emphasisWord).Count();

            if(howBadDoYouWantContent == 0)
                    return new List<ContentData>();

            return _loader
                .GetDescendents(ContentReference.RootPage)
                .OrderByDescending(c => UrlResolver.Current.GetUrl(c))
                .Take(howBadDoYouWantContent)
                .Select(c => _loader.Get<ContentData>(c));
        }

        public IEnumerable<ValidationError> GetParseErrors(string query)
        {
            if (query.Trim().EndsWith("!"))
            {
                return null; // We're good
            }

            return new List<ValidationError>()
            {
                new ValidationError()
                {
                    ErrorMessage = "You need more drama",
                    PropertyName = "Query",
                    Severity = ValidationErrorSeverity.Error,
                    ValidationType = ValidationErrorType.PropertyValidation
                }
            };
        }
    }
}
