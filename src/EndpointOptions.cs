using DeaneBarker.Optimizely.Endpoints.Transformers;

namespace DeaneBarker.Optimizely.Endpoints
{
    public static class EndpointOptions
    {
        public static Dictionary<string, IQueryProcessor> Processors { get; set; } =  new Dictionary<string, IQueryProcessor>();
        public static Dictionary<string, ITransformer> Transformers { get; set; } =  new Dictionary<string, ITransformer>();
    }
}
