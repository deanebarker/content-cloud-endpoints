namespace DeaneBarker.Optimizely.Endpoints
{
    public interface IQueryProcessor
    {
        object GetData(string query, IContent content);
        IEnumerable<string> GetParseErrors(string query);
    }
}
