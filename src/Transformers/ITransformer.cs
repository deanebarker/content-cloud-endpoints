

using EPiServer.Validation;

namespace DeaneBarker.Optimizely.Endpoints.Transformers
{
    public interface ITransformer
    {
        string Transform(string source, object model);
        IEnumerable<ValidationError> GetParseErrors(string source);
    }
}
