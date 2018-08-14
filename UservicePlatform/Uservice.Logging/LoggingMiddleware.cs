using LibOwin;
using Serilog;
using Serilog.Context;
using System;
using System.Diagnostics;
// signature of the OWIN AppFunc
using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

namespace Uservice.Logging
{
    /// <summary>
    /// Middleware for request and response logging
    /// </summary>
    public class RequestLogging
    {
        public static AppFunc Middleware(AppFunc next, ILogger log)
        {
            return async env =>
            {
                var owinContext = new OwinContext(env);

                log.Information("Incoming request: {@Method}, {@Path}, {@Headers}",
                                owinContext.Request.Method,
                                owinContext.Request.Path,
                                owinContext.Request.Headers);

                await next(env).ConfigureAwait(false);

                log.Information("Outgoing response: {@StatusCode}, {@Headers}",
                                owinContext.Response.StatusCode,
                                owinContext.Response.Headers);
            };
        }
    }

    /// <summary>
    /// Middleware for performance logging
    /// </summary>
    public class PerformanceLogging
    {
        public static AppFunc Middleware(AppFunc next, ILogger log)
        {
            return async env =>
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();

                await next(env).ConfigureAwait(false);

                stopWatch.Stop();
                var owinContext = new OwinContext(env);
                log.Information("Request: {@Method} {@Path} executed in {RequestTime:000} ms",
                                owinContext.Request.Method, owinContext.Request.Path,
                                stopWatch.ElapsedMilliseconds);
            };
        }
    }

    /// <summary>
    /// Middleware for creating and reading correlation tokens
    /// </summary>
    public class CorrelationToken
    {
        public static AppFunc Middleware(AppFunc next)
        {
            return async env =>
            {
                var owinContext = new OwinContext(env);
                // Tries to find a correlation token in the request header
                if (!(owinContext.Request.Headers["Correlation-Token"] != null &&
                      Guid.TryParse(owinContext.Request.Headers["Correlation-Token"], out Guid correlationToken)))
                {
                    // not found, creating a new one
                    correlationToken = Guid.NewGuid();
                }

                // Saves the correlation token for later use
                // e.g. adding correlation token for outgoing requests to other uservices
                owinContext.Set("correlationToken", correlationToken.ToString());

                // Adds the correlation token to the log context
                using (LogContext.PushProperty("CorrelationToken", correlationToken))
                {
                    await next(env).ConfigureAwait(false);
                }
            };
        }
    }

    /// <summary>
    /// Middleware that catches and logs all otherwise unhandled exceptions
    /// </summary>
    public class GlobalErrorLogging
    {
        public static AppFunc Middleware(AppFunc next, ILogger log)
        {
            return async env =>
            {
                try
                {
                    await next(env).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    log.Error(ex, "Unhandled exception");
                }
            };
        }
    }
}
