namespace DeaneBarker.Optimizely.Endpoints
{
    public interface IQueryProcessor
    {
        object GetData(string query);
        IEnumerable<string> GetParseErrors(string query);
    }
}
