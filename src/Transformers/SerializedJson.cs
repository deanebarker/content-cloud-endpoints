using EPiServer.ContentApi.Core.Configuration;
using EPiServer.ContentApi.Core.Serialization;
using EPiServer.ContentApi.Core.Serialization.Internal;
using EPiServer.ContentApi.Core.Serialization.Models;
using EPiServer.DataAccess.Internal;
using EPiServer.ServiceLocation;
using EPiServer.Validation;
using EPiServer.Web;
using Parlot.Fluent;
using System.Text.Json;

namespace DeaneBarker.Optimizely.Endpoints.Transformers
{
    public class SerializedJson : ITransformer
    {
        public IEnumerable<ValidationError> GetParseErrors(string source)
        {
            if (!string.IsNullOrWhiteSpace(source))
            {
                return new List<ValidationError>()
                {
                    new ValidationError()
                    {
                        ErrorMessage = "When using serialized JSON, the provided template will be ignored.",
                        Severity = ValidationErrorSeverity.Warning,
                        PropertyName = "Template",
                        ValidationType = ValidationErrorType.PropertyValidation
                    }
                };
            }

            return null;
        }

        public string Transform(string source, object model)
        {
            if (model == null)
                throw new ArgumentNullException("Model cannot be null");

            var contentConvertingService = ServiceLocator.Current.GetInstance<ContentConvertingService>();
            
            var toSerialize = new List<object>();
            foreach(var content in ((IEnumerable<ContentData>)model))
            {
                var converterContext = new ConverterContext(
                    contentReference: ((IContent)content).ContentLink,
                    language: System.Globalization.CultureInfo.CurrentCulture,
                    contentApiOptions: new ContentApiOptions("", false, false, ""),
                    contextMode: ContextMode.Default,
                    select: "",
                    expand: "",
                    excludePersonalizedContent: false
                    );
                toSerialize.Add(contentConvertingService.ConvertToContentApiModel((IContent)content, converterContext));
            }

            var serializer = new Newtonsoft.Json.JsonSerializer();
            var sw = new StringWriter();
            serializer.Serialize(sw, toSerialize);
            return sw.ToString();

        }
    }
}
