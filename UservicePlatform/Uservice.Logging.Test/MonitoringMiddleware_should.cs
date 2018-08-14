using LibOwin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Uservice.Logging.Test
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class MonitoringMiddleware_should
    {
        // no-operation AppFunc
        private readonly AppFunc m_NoOp = env => Task.FromResult(0);

        [Fact]
        public void Return204_for_shallow_path()
        {
            var ctx = SetupOwinTestEnvironment("/monitor/shallow");
            AppFunc pipelineFunc(AppFunc next) => new MonitoringMiddleware(next, null).Invoke;

            // Constructs a pipeline of the middleware under test and the no-operation AppFunc
            var pipeline = pipelineFunc(m_NoOp);

            // Invokes the pipeline with the middleware under test
            var env = ctx.Environment;
            pipeline(env);

            Assert.Equal(204, ctx.Response.StatusCode);
        }

        [Theory]
        [InlineData(204, true)]
        [InlineData(503, false)]
        public void Return204or503_for_deep_path(int expectedStatus, bool isServiceHealthy)
        {
            var ctx = SetupOwinTestEnvironment("/monitor/deep");

            AppFunc pipelineFunc(AppFunc next) => new MonitoringMiddleware(next, HealthCheckFunc(isServiceHealthy)).Invoke;
            var pipeline = pipelineFunc(m_NoOp);
            var env = ctx.Environment;
            pipeline(env);

            Assert.Equal(expectedStatus, ctx.Response.StatusCode);
        }

        private static OwinContext SetupOwinTestEnvironment(string requestPath, string requestMethod = "GET", object requestBody = null)
        {
            var ctx = new OwinContext();
            ctx.Request.Scheme = LibOwin.Infrastructure.Constants.Https;
            ctx.Request.Path = new PathString(requestPath);
            ctx.Request.Method = requestMethod;
            return ctx;
        }

        private static Func<Task<bool>> HealthCheckFunc(bool result)
        {
            return () => Task.FromResult(result);
        }
    }
}
