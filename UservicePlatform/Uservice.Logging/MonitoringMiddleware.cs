using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LibOwin;
// signature of the OWIN AppFunc
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace Uservice.Logging
{
    public class MonitoringMiddleware
    {
        private readonly AppFunc m_Next;
        private readonly Func<Task<bool>> m_HealthCheck;

        // paths of monitoring endpoints
        private static readonly PathString MonitorPath = new PathString("/monitor");
        private static readonly PathString MonitorShallowPath = new PathString("/monitor/shallow");
        private static readonly PathString MonitorDeepPath = new PathString("/monitor/deep");

        private const int NoContentStatusCode = 204;
        private const int ServiceUnavailableStatusCode = 503;

        public MonitoringMiddleware(AppFunc next, Func<Task<bool>> healthCheck)
        {
            m_Next = next;
            m_HealthCheck = healthCheck;
        }

        /// <summary>
        /// Monitoring middleware AppFunc implementation that can be added to an OWIN pipeline
        /// </summary>
        public Task Invoke(IDictionary<string, object> env)
        {
            var context = new OwinContext(env);

            if (context.Request.Path.StartsWithSegments(MonitorPath))
            {
                return HandleMonitorEndpoint(context);
            }

            // Invokes the rest of the pipeline if the request isn’t for a monitoring endpoint
            return m_Next(env);
        }

        private Task HandleMonitorEndpoint(OwinContext context)
        {
            if (context.Request.Path.StartsWithSegments(MonitorShallowPath))
            {
                return ShallowEndpoint(context);
            }

            if (context.Request.Path.StartsWithSegments(MonitorDeepPath))
            {
                return DeepEndpoint(context);
            }

            return Task.FromResult(0);
        }

        private async Task DeepEndpoint(OwinContext context)
        {
            var isServiceHealthy = await m_HealthCheck().ConfigureAwait(false);
            context.Response.StatusCode = isServiceHealthy ? NoContentStatusCode : ServiceUnavailableStatusCode;
        }

        private Task ShallowEndpoint(OwinContext context)
        {
            context.Response.StatusCode = NoContentStatusCode;
            return Task.FromResult(0);
        }
    }
}
