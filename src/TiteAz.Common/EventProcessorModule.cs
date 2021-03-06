using System.Reflection;
using Autofac;
using NEvilES;
using NEvilES.DataStore;
using NEvilES.Pipeline;

namespace TiteAz.Common
{
    public class EventProcessorModule : Autofac.Module
    {
        private readonly Assembly domain;
        private readonly Assembly readModel;

        public EventProcessorModule(Assembly domainAssembly, Assembly readModelAssembly)
        {
            domain = domainAssembly;
            readModel = readModelAssembly;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // NEW

            var version = Assembly.GetEntryAssembly().GetName().Version.ToString();
            builder.RegisterAssemblyTypes(domain).AsClosedTypesOf(typeof(IProcessCommand<>));
            builder.RegisterAssemblyTypes(domain).AsClosedTypesOf(typeof(IHandleStatelessEvent<>));
            builder.RegisterAssemblyTypes(domain).AsClosedTypesOf(typeof(IHandleAggregateCommandMarker<>));
            builder.RegisterAssemblyTypes(domain).AsClosedTypesOf(typeof(INeedExternalValidation<>));

            //builder.RegisterSource(new ContravariantRegistrationSource());

            builder.RegisterAssemblyTypes(readModel).AsClosedTypesOf(typeof(IProject<>));
            builder.RegisterAssemblyTypes(readModel).AsClosedTypesOf(typeof(IProjectWithResult<>));

            builder.RegisterType<PipelineProcessor>().As<ICommandProcessor>().InstancePerLifetimeScope();
            builder.RegisterType<SecurityContext>().As<ISecurityContext>().InstancePerLifetimeScope();
            builder.RegisterType<DatabaseEventStore>().AsImplementedInterfaces().InstancePerLifetimeScope();
            builder.RegisterType<Factory>().As<IFactory>().InstancePerLifetimeScope();
            builder.RegisterType<PipelineTransaction>().As<CommandContext.ITransaction>().AsSelf().InstancePerLifetimeScope();

            builder.Register(c =>
            {
                var transaction = c.Resolve<CommandContext.ITransaction>();
                var user = c.ResolveNamed<CommandContext.IUser>("user");
                var impersonatedBy = c.ResolveOptionalNamed<CommandContext.IUser>("impersonator");
                return new CommandContext(user, transaction, impersonatedBy, version);
            }).As<CommandContext>().InstancePerLifetimeScope();
        }
    }

}