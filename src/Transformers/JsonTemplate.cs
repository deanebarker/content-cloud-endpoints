using EPiServer.Validation;
using System.Collections;
using System.Text.Json;

namespace DeaneBarker.Optimizely.Endpoints.Transformers
{
    public class JsonTemplate : ITransformer
    {
        public IEnumerable<ValidationError> GetParseErrors(string source)
        {
            return null;
        }

        public string Transform(string source, object model)
        {
            var output = new List<object>();
            foreach(var item in model as IEnumerable)
            {
                output.Add(ObjectTranslator.ObjectTranslator.Translate(source, item));
            }

            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(output, serializeOptions);
        }
    }
}
