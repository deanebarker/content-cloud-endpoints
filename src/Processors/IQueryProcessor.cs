using EPiServer.Validation;

namespace DeaneBarker.Optimizely.Endpoints
{
    public interface IQueryProcessor
    {
        object GetData(string query, IContent content);
        IEnumerable<ValidationError> GetParseErrors(string query);
    }
}
