using Nancy;
using Nancy.Bootstrapper;
using Nancy.Configuration;
using Nancy.TinyIoc;
using Serilog;

namespace Uservice.Platform
{
    /// <summary>
    /// A default Bootstrapper used across the UService system
    /// </summary>
    public class UserviceDefaultBootstrapper : DefaultNancyBootstrapper
    {
        protected readonly ILogger Logger;

        public UserviceDefaultBootstrapper(ILogger logger)
        {
            Logger = logger;
        }

        public override void Configure(INancyEnvironment environment)
        {
            environment.Tracing(enabled: false, displayErrorTraces: true);
        }

        /// <summary>
        /// Registers the logger to Nancy's container for later use
        /// </summary>
        protected override void ApplicationStartup(TinyIoCContainer container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);
            container.Register(Logger);
            container.UseHttpClient(new NancyContext()); // dummy for Module discovering while Application startup
        }

        /// <summary>
        /// Registers the UserviceHttpClient as a per-request dependency in Nancy’s container
        /// </summary>
        protected override void RequestStartup(
          TinyIoCContainer container,
          IPipelines pipelines,
          NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);
            container.UseHttpClient(context);
        }
    }
}
