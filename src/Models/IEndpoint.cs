namespace DeaneBarker.Optimizely.Endpoints
{
    public interface IEndpoint
    {
        IQueryProcessor QueryProcessor { get; }
        string QuerySource { get; }
    }
}
