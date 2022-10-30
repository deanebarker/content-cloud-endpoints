namespace DeaneBarker.Optimizely
{
    public interface IContentValueProvider
    {
        object GetValue(IContent content, string field);
        bool FieldExists(string field);
    }
}