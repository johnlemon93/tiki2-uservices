using Serilog;
using Serilog.Sinks.TestCorrelator;
using Xunit;
using FluentAssertions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using LibOwin;
using Serilog.Events;
using System.Linq;

namespace Uservice.Logging.Test
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class LoggingMiddleware_should
    {
        private const string TestPath = "/test/path";
        private const string ErrorPath = "/error";

        // no-operation AppFunc
        private readonly AppFunc m_NoOp = env => Task.FromResult(0);

        // handle a simple request
        private readonly Func<AppFunc, ILogger, AppFunc> m_TestModule =
         (next, log) => async env =>
         {
             var ctx = new OwinContext(env);
             if (ctx.Request.Path.Value == ErrorPath)
             {
                 throw new Exception();
             }

             log.Information("I got a request!");

             if (ctx.Request.Path.Value == TestPath)
             {
                 await Task.Delay(2000).ConfigureAwait(false);
                 ctx.Response.StatusCode = 404;
             }
             else
             {
                 await next(env).ConfigureAwait(false);
             }
         };

        private readonly ILogger m_Logger = new LoggerConfiguration()
            .Enrich.FromLogContext()
            .WriteTo.TestCorrelator()
            .CreateLogger();

        [Fact]
        public void Log_each_incoming_request_and_each_outgoing_response()
        {
            using (TestCorrelator.CreateContext())
            {
                AppFunc pipelineFunc(AppFunc next) => RequestLogging.Middleware(next, m_Logger);
                var ctx = SetupOwinTestEnvironment("/test");

                var pipeline = pipelineFunc(m_NoOp);
                var env = ctx.Environment;
                pipeline(env);

                var msgs = TestCorrelator.GetLogEventsFromCurrentContext();
                msgs.Should().NotBeEmpty().And.HaveCount(2);

                var reqLogMsg = msgs.First();
                reqLogMsg.Level.Should().Be(LogEventLevel.Information);
                reqLogMsg.RenderMessage().Should().Be("Incoming request: \"GET\", PathString { Value: \"/test\", HasValue: True }, []");

                var resLogMsg = msgs.Last();
                resLogMsg.Level.Should().Be(LogEventLevel.Information);
                resLogMsg.RenderMessage().Should().Be("Outgoing response: 200, []");
            }
        }

        [Fact]
        public async void Log_execution_time_of_the_request()
        {
            using (TestCorrelator.CreateContext())
            {
                AppFunc pipelineFunc(AppFunc next) => PerformanceLogging.Middleware(next, m_Logger);
                var ctx = SetupOwinTestEnvironment(TestPath);


                var pipeline = pipelineFunc(m_TestModule(m_NoOp, m_Logger));
                var env = ctx.Environment;
                await pipeline(env).ConfigureAwait(false);

                var msg = TestCorrelator.GetLogEventsFromCurrentContext()
                    .Should().HaveCount(2)
                    .And.Contain(m => m.MessageTemplate.Text == "Request: {@Method} {@Path} executed in {RequestTime:000} ms");

                msg.Which.Level.Should().Be(LogEventLevel.Information);
                msg.Which.Properties.Should().Contain(p => p.Key == "RequestTime" && int.Parse(p.Value.ToString()) > 2000);
            }
        }

        [Theory]
        [InlineData("/test")]
        [InlineData(TestPath)]
        public void Add_correlation_token_to_log_context_and_Owin_context(string requestPath)
        {
            using (TestCorrelator.CreateContext())
            {
                AppFunc pipelineFunc(AppFunc next) => CorrelationToken.Middleware(next);
                var ctx = SetupOwinTestEnvironment(requestPath);

                var pipeline = pipelineFunc(m_TestModule(m_NoOp, m_Logger));
                var env = ctx.Environment;
                pipeline(env);

                // m_Logger was enriched from the log context so it's properties should include CorrelationToken
                var tokenProp = TestCorrelator.GetLogEventsFromCurrentContext().Should().ContainSingle()
                    .Which.Properties.Should().ContainSingle();
                tokenProp.Which.Key.Should().Be("CorrelationToken");

                // the token was also saved in OwinContext with the same value
                tokenProp.Which.Value.ToString().Should().Equals(ctx.Get<string>("correlationToken"));

                // the token should be from request header if it is available there
                var tokenFromRequestHeader = ctx.Request.Headers["Correlation-Token"];
                if (!string.IsNullOrEmpty(tokenFromRequestHeader))
                {
                    tokenProp.Which.Value.ToString().Should().Equals(tokenFromRequestHeader);
                }
            }
        }

        [Fact]
        public void Catch_and_log_unhandled_exception()
        {
            using (TestCorrelator.CreateContext())
            {
                AppFunc pipelineFunc(AppFunc next) => GlobalErrorLogging.Middleware(next, m_Logger);
                var ctx = SetupOwinTestEnvironment(ErrorPath);

                var pipeline = pipelineFunc(m_TestModule(m_NoOp, m_Logger));
                var env = ctx.Environment;
                pipeline(env);

                var msg = TestCorrelator.GetLogEventsFromCurrentContext().Should().ContainSingle();
                msg.Which.Level.Should().Be(LogEventLevel.Error);
            }
        }

        private static OwinContext SetupOwinTestEnvironment(string requestPath)
        {
            var ctx = new OwinContext();
            ctx.Request.Scheme = LibOwin.Infrastructure.Constants.Https;
            ctx.Request.Path = new PathString(requestPath);

            if (requestPath == TestPath)
            {
                ctx.Request.Headers.Append("Correlation-Token", Guid.NewGuid().ToString());
            }

            ctx.Request.Method = "GET";
            return ctx;
        }

    }
}
