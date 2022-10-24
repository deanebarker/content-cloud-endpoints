using Alloy.Liquid.Liquid.Models.Blocks;
using EPiServer.Shell.ObjectEditing;

namespace DeaneBarker.Optimizely.Endpoints.Models
{
    [ContentType(
        GroupName = "Development",
        GUID = "AEECADB2-3E89-4117-ADEB-F8D43565D2F4",
        AvailableInEditMode = true,
        DisplayName = "Content Endpoint"
        )
    ]
    public class EndpointPage : PageData
    {
        [ClientEditor(ClientEditingClass = "/js/editor.js")]
        public virtual string Query { get; set; }

        [ValidateLiquidParse]
        [ClientEditor(ClientEditingClass = "/js/editor.js")]
        public virtual string Template { get; set; }

        public override void SetDefaultValues(ContentType contentType)
        {
            base.SetDefaultValues(contentType);
            VisibleInMenu = false;
        }
    }
}
