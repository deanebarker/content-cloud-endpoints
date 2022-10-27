using DeaneBarker.Optimizely.Endpoints.Transformers;
using DeaneBarker.Optimizely.Endpoints.TreeQL;
using EPiServer.Shell.ObjectEditing;
using Fluid;
using Optimizely.CMS.Labs.LiquidTemplating.ViewEngine;
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

        public string QuerySource => Query;
        public IQueryProcessor QueryProcessor => new TreeQlQueryProcessor();
        public string TemplateSource => Template;
        public ITransformer Transformer => new LiquidTemplater();
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
    public class ValidateLiquidParse : ValidationAttribute
    {
        public ValidateLiquidParse()
        {

        }

        public override bool IsValid(object value)
        {
            return GetParseException(value.ToString()) == null;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (string.IsNullOrWhiteSpace(value?.ToString()))
            {
                return ValidationResult.Success;
            }

            var message = GetParseException(value?.ToString());
            if (message != null)
            {
                return new ValidationResult($"Liquid parse error: {message}");
            }

            return ValidationResult.Success;
        }

        private string GetParseException(string liquidCode)
        {
            var parser = new CmsFluidViewParser(new FluidParserOptions() { AllowFunctions = true });

            try
            {
                var template = parser.Parse(liquidCode);
            }
            catch (Exception e)
            {
                return e.Message;
            }

            return null;
        }
    }

}
