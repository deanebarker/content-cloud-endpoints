using EPiServer.Validation;
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
            return JsonSerializer.Serialize(((IEnumerable<ContentData>)model).Select(c => getSimplifiedObject((IContent)c)));

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
    }
}
