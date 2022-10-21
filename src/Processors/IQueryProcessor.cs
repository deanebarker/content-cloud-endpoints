namespace Alloy.Liquid.Endpoints.Processors
{
    public interface IQueryProcessor
    {
        object GetData(string query);
        IEnumerable<string> GetParseErrors(string query);
    }
}
