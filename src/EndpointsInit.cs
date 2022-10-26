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
        }

        public void Uninitialize(InitializationEngine context)
        {
            
        }
    }
}
