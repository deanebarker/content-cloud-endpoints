using Alloy.Liquid.Models.Pages;
using DeaneBarker.Optimizely.Endpoints.Transformers;
using DeaneBarker.Optimizely.Endpoints.TreeQL;
using EPiServer.ServiceLocation;
using EPiServer.Shell.ObjectEditing;
using EPiServer.Validation;
using System.ComponentModel.DataAnnotations;

namespace DeaneBarker.Optimizely.Endpoints.Models
{
    [ContentType(
        GroupName = "Development",
        GUID = "AEECADB2-3E89-4117-ADEB-F8D43565D2F4",
        AvailableInEditMode = true,
        DisplayName = "Content Endpoint"
        )
    ]

    public class EndpointPage : PageData, IEndpoint
    {
        public string QuerySource => Query;
        public IQueryProcessor QueryProcessor => EndpointOptions.Processors.GetValueOrDefault(QueryProcessorName) ?? new TreeQlQueryProcessor();
        public string TemplateSource => Template;
        public ITransformer Transformer => EndpointOptions.Transformers.GetValueOrDefault(TransformerName) ?? new LiquidTemplater();



        [Display(Name = "Query Processor", GroupName = "Data Configuration", Order = 100)]
        [SelectOne(SelectionFactoryType = typeof(ProcessorSelectionFactory))]
        public virtual string QueryProcessorName { get; set; }


        [Display(Name = "Query Text", GroupName = "Data Configuration", Order = 200)]
        [ClientEditor(ClientEditingClass = "/js/editor.js")]
        public virtual string Query { get; set; }


        [Display(Name = "Transformer", GroupName = "Data Configuration", Order = 300)]
        [SelectOne(SelectionFactoryType = typeof(TransformerSelectionFactory))]
        public virtual string TransformerName { get; set; }


        [Display(Name = "Transformer Text", GroupName = "Data Configuration", Order = 400)]
        [ClientEditor(ClientEditingClass = "/js/editor.js")]
        public virtual string Template { get; set; }



        public override void SetDefaultValues(ContentType contentType)
        {
            base.SetDefaultValues(contentType);
            QueryProcessorName = EndpointOptions.Processors.FirstOrDefault().Key;
            TransformerName = EndpointOptions.Transformers.FirstOrDefault().Key;
            VisibleInMenu = false;
        }
    }

    public class ProcessorSelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            return EndpointOptions.Processors.Select(p => new SelectItem() { Text = p.Key, Value = p.Key });
        }
    }

    public class TransformerSelectionFactory : ISelectionFactory
    {
        public IEnumerable<ISelectItem> GetSelections(ExtendedMetadata metadata)
        {
            return EndpointOptions.Transformers.Select(p => new SelectItem() { Text = p.Key, Value = p.Key });
        }
    }

    public class EndpointValidator : IValidate<IEndpoint>
    {
        IEnumerable<ValidationError> IValidate<IEndpoint>.Validate(IEndpoint endpoint)
        {
            var errors = new List<ValidationError>();

            errors.AddRange(endpoint.QueryProcessor.GetParseErrors(endpoint.QuerySource) ?? Enumerable.Empty<ValidationError>());
            errors.AddRange(endpoint.Transformer.GetParseErrors(endpoint.QuerySource) ?? Enumerable.Empty<ValidationError>());

            return errors;
        }
    }
}
