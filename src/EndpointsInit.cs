using DeaneBarker.Optimizely.Endpoints.Processoers;
using DeaneBarker.Optimizely.Endpoints.Transformers;
using DeaneBarker.Optimizely.Endpoints.TreeQL;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using System.ComponentModel;

namespace DeaneBarker.Optimizely.Endpoints
{
    [InitializableModule]
    [ModuleDependency(typeof(EPiServer.Web.InitializationModule))]
    public class EndpointsInit : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            context.Services.AddSingleton<IContentValueProvider, ContentValueProvider>();
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

            TreeQueryParser.AllowedOperators = new[] { "=", "!=", ">", ">=", "<", "<=", "contains", "startswith", "starts", "endswith", "ends" };

            // This is wrong. I need to use the right pattern
            EndpointOptions.Processors.Add("Teenager QL", new TeenagerQL());
            EndpointOptions.Processors.Add("Content QL", new TreeQlQueryProcessor());

            EndpointOptions.Transformers.Add("Serialized JSON", new SerializedJson());
            EndpointOptions.Transformers.Add("Liquid", new LiquidTemplater());
        }

        public void Uninitialize(InitializationEngine context)
        {
            
        }
    }
}
