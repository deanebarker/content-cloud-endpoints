using EPiServer.ServiceLocation;
using EPiServer.Validation;
using Fluid;
using Fluid.Values;
using Newtonsoft.Json.Linq;
using System.Dynamic;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DeaneBarker.Optimizely.Endpoints.Transformers
{
    public class JsonTemplate : ITransformer
    {
        public IEnumerable<ValidationError> GetParseErrors(string source)
        {
            return null;
        }

        public string Transform(string source, object model)
        {
            var convertor = new JsonObjectConvertor(source);
            return new JsonArrayConvertor((IEnumerable<ContentData>)model).ToString(convertor);
        }
    }

    public class JsonArrayConvertor
    {
        public List<object> Elements { get; set; } = new List<object>();

        public JsonArrayConvertor(IEnumerable<object> elements)
        {
            Elements = elements.ToList();
        }

        public IEnumerable<ExpandoObject> ToObjects(JsonObjectConvertor convertor)
        {
            var elements = new List<ExpandoObject>();
            Elements.ForEach(o => elements.Add(convertor.ToObject(o)));
            return elements;
        }

        public string ToString(JsonObjectConvertor convertor)
        {
            return JsonSerializer.Serialize(ToObjects(convertor));
        }
    }

    public class JsonObjectConvertor
    {
        public List<JsonPropertyConvertor> PropertyConvertors { get; set; } = new List<JsonPropertyConvertor>();

        public JsonObjectConvertor(string input)
        {
            if(string.IsNullOrWhiteSpace(input))
            {
                return; // Empty template, means no property convertors...
            }
            input.Trim().Split(new string[] { "\n", "\r\n", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList().ForEach(l => AddProperty(l));
        }

        public ExpandoObject ToObject(object model)
        {
            var obj = new ExpandoObject();
            foreach (var prop in PropertyConvertors)
            {
                var data = prop.GetData(model);
                ((IDictionary<String, Object>)obj).Add(data.propertyName, data.propertyValue);
            }
            return obj;
        }

        public void AddProperty(string input)
        {
            PropertyConvertors.Add(new JsonPropertyConvertor(input));
        }
    }

    public class JsonPropertyConvertor
    {
        private static FluidParser _parser;
        private static TemplateOptions _options;

        public string PropertyName { get; set; }
        public string OutputTemplate { get; set; }

        static JsonPropertyConvertor()
        {
            _options = new TemplateOptions() { MemberAccessStrategy = new UnsafeMemberAccessStrategy() };
            _parser = new FluidParser();
        }

        public JsonPropertyConvertor(string input)
        {
            if (!input.Contains(":"))
            {
                PropertyName = input;
                OutputTemplate = "{{ " + input + " }}";
            }

            if(input.Contains(":"))
            {
                PropertyName = input.Split(":".ToCharArray()).First().Trim();
                OutputTemplate = "{{ " + input.Substring(input.IndexOf(":") + 1).Trim() + " }}";
            }
        }

        public (string propertyName, string propertyValue) GetData(object model)
        {
            var template = _parser.Parse(OutputTemplate);
            var context = new TemplateContext(new JsonConversionValue(model), _options);
            var value = template.Render(context);
            return (PropertyName, value);
        }

        public string ToString(object model)
        {
            var templateString = "\"" + PropertyName + "\": \"" + OutputTemplate + "\"";
            var template = _parser.Parse(templateString);
            var context = new TemplateContext();
            context.SetValue("data", new JsonConversionValue(model));
            return template.Render(context);
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
            if(providedValue == null)
                return NilValue.Instance;

            if (providedValue is DateTime)
                return DateTimeValue.Create(providedValue, TemplateOptions.Default);

            if (providedValue is int)
                return NumberValue.Create(providedValue, TemplateOptions.Default);

            return StringValue.Create(providedValue.ToString());
        }
    }
}
