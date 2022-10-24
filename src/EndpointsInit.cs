using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;

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
            
        }

        public void Uninitialize(InitializationEngine context)
        {
            
        }
    }
}
