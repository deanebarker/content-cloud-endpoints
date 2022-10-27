using DeaneBarker.Optimizely.Endpoints.Transformers;

namespace DeaneBarker.Optimizely.Endpoints
{
    public interface IEndpoint
    {
        IQueryProcessor QueryProcessor { get; }
        string QuerySource { get; }
        string TemplateSource { get; }
        ITransformer Transformer { get; }
    }
}
