using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nancy;
using Nancy.Bootstrapper;
using Nancy.Owin;
using Nancy.TinyIoc;
using Serilog;
using System.Threading.Tasks;
using Uservice.Logging;
using Uservice.Platform;

namespace ProductCatalog
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            var logger = ConfigureLogger();

            app.UseOwin()
                .UseMonitoringAndLogging(logger, HealthCheck)
                .UseNancy(opt => opt.Bootstrapper = new ProductCatalogBootStrapper(logger));
        }

        private ILogger ConfigureLogger()
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        private static Task<bool> HealthCheck() => Task.FromResult(true);
    }

    public class ProductCatalogBootStrapper : UserviceDefaultBootstrapper
    {
        public ProductCatalogBootStrapper(ILogger logger) : base(logger)
        {
        }

        protected override void RequestStartup(TinyIoCContainer container, IPipelines pipelines, NancyContext context)
        {
            base.RequestStartup(container, pipelines, context);

            //CORS Enable https://stackoverflow.com/questions/15658627/is-it-possible-to-enable-cors-using-nancyfx/29322285#29322285
            pipelines.AfterRequest.AddItemToEndOfPipeline((ctx) =>
            {
                ctx.Response.WithHeader("Access-Control-Allow-Origin", "*")
                            .WithHeader("Access-Control-Allow-Methods", "POST,GET")
                            .WithHeader("Access-Control-Allow-Headers", "Accept, Origin, Content-type");

            });
        }
    }

}
