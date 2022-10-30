namespace DeaneBarker.Optimizely.Endpoints.TreeQL
{
    public class LabelProvider : ILabelProvider
    {
        public static string LabelPropertyName { get; set; } = "ContentLabel";
        private IContentLoader _loader;
        public LabelProvider(IContentLoader loader)
        {
            _loader = loader;
        }

        public ContentReference GetContentForLabel(string label)
        {
            // Written out longhand...
            return _loader
                .GetDescendents(ContentReference.RootPage)
                .Select(c => _loader.Get<IContent>(c)) // Get all content
                .Where(c => c.Property[LabelPropertyName] != null) // Where it has the property
                .Where(c => c.Property[LabelPropertyName].Value != null) // Where the property has a value
                .FirstOrDefault(c => c.Property[LabelPropertyName].Value.ToString().Trim().ToLower() == label.ToLower().Trim()) // Where the property equals the label
                ?.ContentLink; // Return the reference or null
        }
    }
}
