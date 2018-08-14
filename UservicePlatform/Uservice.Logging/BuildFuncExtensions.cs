using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Serilog;

namespace Uservice.Logging
{
    using BuildFunc = Action<Func<
        Func<IDictionary<string, object>, Task>,
        Func<IDictionary<string, object>, Task>>>;

    public static class BuildFuncExtensions
    {
        public static BuildFunc UseMonitoringAndLogging(
          this BuildFunc buildFunc,
          ILogger log,
          Func<Task<bool>> healthCheck)
        {
            buildFunc(next => GlobalErrorLogging.Middleware(next, log));
            buildFunc(next => CorrelationToken.Middleware(next));
            buildFunc(next => RequestLogging.Middleware(next, log));
            buildFunc(next => PerformanceLogging.Middleware(next, log));
            buildFunc(next => new MonitoringMiddleware(next, healthCheck).Invoke);

            return buildFunc;
        }
    }
}
