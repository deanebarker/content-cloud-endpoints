namespace DeaneBarker.Optimizely.Endpoints.TreeQL
{
    public interface ILabelProvider
    {
        ContentReference GetContentForLabel(string label);
    }
}
