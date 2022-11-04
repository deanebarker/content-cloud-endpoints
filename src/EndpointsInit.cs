using DeaneBarker.ObjectTranslator;
using DeaneBarker.Optimizely.Endpoints.Processoers;
using DeaneBarker.Optimizely.Endpoints.Transformers;
using DeaneBarker.Optimizely.Endpoints.TreeQL;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using Fluid;
using Fluid.Values;
using System.ComponentModel;
using System.Globalization;
using System.Text.Encodings.Web;
using DeaneBarker.TreeQL;

namespace DeaneBarker.Optimizely.Endpoints
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class EndpointsInit : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IContentValueProvider, ContentValueProvider>();
            context.Services.AddSingleton<ILabelProvider, LabelProvider>();
        }

        public void Initialize(InitializationEngine context)
        {
            TreeQueryParser.TargetValidator = (t) =>
            {
                if (t.ToString().StartsWith("/") && t.ToString().EndsWith("/"))
                    return true;

                if (t.ToString().StartsWith("@"))
                    return true;

                if (t.ToString().All(c => Char.IsDigit(c)))
                    return true;

                return false;
            };


            TreeQueryParser.TargetValidatorError = "Target must (1) begin and end with a forward slash, (2) be an integer, or (3) start with \"@\"";
            TreeQueryParser.AllowedOperators = new[] { "=", "!=", ">", ">=", "<", "<=", "contains", "startswith", "starts", "endswith", "ends" };

            // This is wrong. I need to use the right pattern
            EndpointOptions.Processors.Add("Content QL", new TreeQlQueryProcessor());
            EndpointOptions.Processors.Add("Teenager QL", new TeenagerQL());

            EndpointOptions.Transformers.Add("Serialized JSON", new SerializedJson());
            EndpointOptions.Transformers.Add("Templated JSON", new JsonTemplate());
            EndpointOptions.Transformers.Add("Liquid HTML", new LiquidTemplater());

            ValueGenerator.ValueExtractor = GetValue;
        }

        public void Uninitialize(InitializationEngine context)
        {
            
        }

        public static object GetValue(object model, string token, string template)
        {
            // Is this a virtual field?
            if (model is IContent content && !string.IsNullOrWhiteSpace(token))
            {
                var _valueProvider = ServiceLocator.Current.GetInstance<IContentValueProvider>();
                if (_valueProvider.FieldExists(token))
                {
                    return _valueProvider.GetValue(content, token);
                }
            }

            // Is this self-referential?
            if (token == "_")
            {
                return model;
            }

            // Can we resolve this?
            if (!string.IsNullOrWhiteSpace(token))
            {
                // This is a simple, resolvable value

                var currentValue = model;
                foreach (var segment in token.Split("."))
                {
                    if (currentValue == null)
                    {
                        return null;
                    }

                    var property = currentValue.GetType().GetProperties().FirstOrDefault(p => p.Name.ToLower() == segment.ToLower());
                    if (property == null)
                    {
                        return null;
                    }

                    currentValue = property.GetValue(currentValue);

                }
                return currentValue;
            }

            // This is a Liquid template expression

            var context = new TemplateContext(new JsonConversionValue(model));
            context.SetValue("_", model);

            TemplateOptions.Default.MemberAccessStrategy = new UnsafeMemberAccessStrategy();
            var templateString = "{{ " + template + " }}";
            var parser = new FluidParser();
            var parsedTemplate = parser.Parse(templateString);

            return parsedTemplate.Render(context);
        }


    }

    public class JsonConversionValue : FluidValue
    {
        private object value;
        private static IContentValueProvider _valueProvider = ServiceLocator.Current.GetInstance<IContentValueProvider>();

        public JsonConversionValue(object value)
        {
            this.value = value;
        }

        public override FluidValues Type => FluidValues.Object;

        public override bool Equals(FluidValue other)
        {
            return false;
        }

        public override bool ToBooleanValue()
        {
            return false;
        }

        public override decimal ToNumberValue()
        {
            throw new NotImplementedException();
        }

        public override object ToObjectValue()
        {
            throw new NotImplementedException();
        }

        public override string ToStringValue()
        {
            throw new NotImplementedException();
        }

        public override void WriteTo(TextWriter writer, TextEncoder encoder, CultureInfo cultureInfo)
        {
            // This should never get called on this...
        }

        protected override FluidValue GetValue(string name, TemplateContext context)
        {
            var providedValue = _valueProvider.GetValue((IContent)value, name);

            if (providedValue == null)
                return NilValue.Instance;

            if (providedValue is DateTime)
                return DateTimeValue.Create(providedValue, TemplateOptions.Default);

            if (providedValue is int)
                return NumberValue.Create(providedValue, TemplateOptions.Default);

            if (providedValue is IEnumerable<object>)
                return ArrayValue.Create(providedValue, TemplateOptions.Default);

            return StringValue.Create(providedValue.ToString());
        }
    }
}
