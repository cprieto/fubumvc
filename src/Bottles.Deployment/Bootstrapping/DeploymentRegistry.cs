using Bottles.Deployment.Directives;
using StructureMap.Configuration.DSL;

namespace Bottles.Deployment.Bootstrapping
{
    public class DeploymentRegistry : Registry
    {
        public DeploymentRegistry()
        {
            Scan(x =>
            {
                //TODO: Add diagnostics to the scanning
                x.AssembliesFromApplicationBaseDirectory(a => a.GetName().Name.Contains("Deployers"));

                //REVIEW: Ugly?
                x.AssemblyContainingType<FubuWebsite>();

                x.ConnectImplementationsToTypesClosing(typeof (IInitializer<>));
                x.ConnectImplementationsToTypesClosing(typeof (IDeployer<>));
                x.ConnectImplementationsToTypesClosing(typeof (IFinalizer<>));
            });

            Scan(x =>
            {
                x.TheCallingAssembly();
                x.WithDefaultConventions();
            });
        }
    }
}