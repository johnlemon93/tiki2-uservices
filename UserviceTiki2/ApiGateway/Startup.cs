using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Nancy.Owin;
using Serilog;
using System.Threading.Tasks;
using Uservice.Logging;
using Uservice.Platform;
using Microsoft.Extensions.Configuration;

namespace ApiGateway
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
                .UseNancy(opt => opt.Bootstrapper = new UserviceDefaultBootstrapper(logger));
        }

        private ILogger ConfigureLogger()
        {
            return new LoggerConfiguration()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();
        }

        private static Task<bool> HealthCheck() => Task.FromResult(true);
    }
}
