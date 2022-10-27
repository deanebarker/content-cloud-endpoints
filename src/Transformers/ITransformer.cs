namespace DeaneBarker.Optimizely.Endpoints.Transformers
{
    public interface ITransformer
    {
        string Transform(string source, object model);
    }
}
