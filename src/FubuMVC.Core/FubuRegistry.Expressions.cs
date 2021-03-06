using System;
using System.Linq;
using FubuCore;
using FubuCore.Reflection;
using FubuMVC.Core.Diagnostics;
using FubuMVC.Core.Registration;
using FubuMVC.Core.Registration.DSL;

namespace FubuMVC.Core
{
    public partial class FubuRegistry : IFubuRegistry
    {
        private DiagnosticLevel _diagnosticLevel = DiagnosticLevel.None;

        public RouteConventionExpression Routes
        {
            get { return new RouteConventionExpression(_routeResolver, this); }
        }

        public OutputDeterminationExpression Output
        {
            get { return new OutputDeterminationExpression(this); }
        }

        public ViewExpression Views
        {
            get { return new ViewExpression(_viewAttacher, this); }
        }

        public PoliciesExpression Policies
        {
            get { return new PoliciesExpression(_policies, _systemPolicies); }
        }

        public ModelsExpression Models
        {
            get { return new ModelsExpression(Services); }
        }

        public AppliesToExpression Applies
        {
            get { return new AppliesToExpression(_types); }
        }

        public ActionCallCandidateExpression Actions
        {
            get { return new ActionCallCandidateExpression(_behaviorMatcher, _types, _actionSourceMatcher); }
        }

        public MediaExpression Media
        {
            get
            {
                return new MediaExpression(this);
            }
        }

        public void ResolveTypes(Action<TypeResolver> configure)
        {
            Services(x =>
            {
                x.SetServiceIfNone<ITypeResolver>(new TypeResolver());
                var resolver = x.FindAllValues<ITypeResolver>().FirstOrDefault() as TypeResolver;

                configure(resolver);
            });
        }

        public void UsingObserver(IConfigurationObserver observer)
        {
            _observer = observer;
        }

        public void Services(Action<IServiceRegistry> configure)
        {
            _serviceRegistrations.Add(configure);
        }

        public void ApplyConvention<TConvention>()
            where TConvention : IConfigurationAction, new()
        {
            ApplyConvention(new TConvention());
        }

        public void ApplyConvention<TConvention>(TConvention convention)
            where TConvention : IConfigurationAction
        {
            _conventions.Add(convention);
        }

        public ChainedBehaviorExpression Route(string pattern)
        {
            var expression = new ExplicitRouteConfiguration(pattern);
            _explicits.Add(expression);

            return expression.Chain();
        }

        public void Import<T>(string prefix) where T : FubuRegistry, new()
        {
            if (_imports.Any(x => x.Registry is T)) return;

            Import(new T(), prefix);
        }

        /// <summary>
        /// Imports the declarations of an IFubuRegistryExtension
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void Import<T>() where T : IFubuRegistryExtension, new()
        {
            new T().Configure(this);
        }

        public void Import(FubuRegistry registry, string prefix)
        {
            _imports.Add(new RegistryImport{
                Prefix = prefix,
                Registry = registry
            });
        }

        public void IncludeDiagnostics(bool shouldInclude)
        {
            _diagnosticLevel = shouldInclude ? DiagnosticLevel.FullRequestTracing : DiagnosticLevel.None;

            if (shouldInclude)
            {
                UsingObserver(new RecordingConfigurationObserver());

                Import<DiagnosticsRegistry>(string.Empty);

                Services(graph => graph.AddService(new DiagnosticsIndicator().SetEnabled()));
            }
            else
            {
                Actions
                    .ExcludeTypes(t => t.HasAttribute<FubuDiagnosticsAttribute>())
                    .ExcludeMethods(call => call.Method.HasAttribute<FubuDiagnosticsAttribute>());
            }
        }

        public DiagnosticLevel DiagnosticLevel
        {
            get
            {
                return _diagnosticLevel;
            }
        }

        /// <summary>
        ///   This allows you to drop down to direct manipulation of the BehaviorGraph
        ///   produced by this FubuRegistry
        /// </summary>
        /// <param name = "alteration"></param>
        public void Configure(Action<BehaviorGraph> alteration)
        {
            addExplicit(alteration);
        }

        #region Nested type: RegistryImport

        public class RegistryImport
        {
            public string Prefix { get; set; }
            public FubuRegistry Registry { get; set; }

            public void ImportInto(IChainImporter graph)
            {
                graph.Import(Registry.BuildGraph(), b =>
                {
                    b.PrependToUrl(Prefix);
                    b.Origin = Registry.Name;
                });
            }
        }

        #endregion
    }


}