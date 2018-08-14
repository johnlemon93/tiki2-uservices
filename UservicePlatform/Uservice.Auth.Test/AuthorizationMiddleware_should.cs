using LibOwin;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace Uservice.Auth.Test
{
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class AuthorizationMiddleware_should
    {
        // no-operation AppFunc
        private readonly AppFunc m_NoOp = env => Task.FromResult(0);

        [Fact]
        public void Call_next_in_the_pipeline_only_if_the_request_has_the_required_scope_otherwise_return403_forbidden()
        {
            AppFunc pipelineFunc(AppFunc next) => Authorization.Middleware(next, "test_scope");

            var pipeline = pipelineFunc(m_NoOp);
            var ctx = SetupOwinTestEnvironment("test_scope");
            var env = ctx.Environment;
            pipeline(env);

            Assert.Equal(200, ctx.Response.StatusCode);

            ctx = SetupOwinTestEnvironment("not_test_scope");
            env = ctx.Environment;
            pipeline(env);

            Assert.Equal(403, ctx.Response.StatusCode);
        }

        [Fact]
        public void Add_the_user_identity_object_to_Owin_context_when_the_request_originates_from_an_end_user_request()
        {
            AppFunc pipelineFunc(AppFunc next) => IdToken.Middleware(next);
            // online generated JWT token with secret key is "secret"
            const string testUserJwt = "eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJPbmxpbmUgSldUIEJ1aWxkZXIiLCJpYXQiOjE1MzQyMjA3ODEsImV4cCI6MTU2NTc1Njc4MSwiYXVkIjoid3d3LmV4YW1wbGUuY29tIiwic3ViIjoianJvY2tldEBleGFtcGxlLmNvbSIsIkdpdmVuTmFtZSI6IkpvaG5ueSIsIlN1cm5hbWUiOiJSb2NrZXQiLCJFbWFpbCI6Impyb2NrZXRAZXhhbXBsZS5jb20iLCJSb2xlIjpbIk1hbmFnZXIiLCJQcm9qZWN0IEFkbWluaXN0cmF0b3IiXX0.XYq-fVo5N-GWgRENl2yxUAc6xqqjkvoUiOf-GY0mQjU";

            var pipeline = pipelineFunc(m_NoOp);
            var ctx = SetupOwinTestEnvironment();
            ctx.Request.Headers.Append("uservice-end-user", testUserJwt);
            var env = ctx.Environment;
            pipeline(env);

            var principle = ctx.Get<ClaimsPrincipal>("pos_end_user");
            Assert.NotNull(principle);
        }

        private static OwinContext SetupOwinTestEnvironment(string scope = "")
        {
            var ctx = new OwinContext();
            ctx.Request.Scheme = LibOwin.Infrastructure.Constants.Https;
            ctx.Request.Path = new PathString("/test");

            ctx.Request.User = new ClaimsPrincipal();
            ctx.Request.User.AddIdentity(new ClaimsIdentity(new List<Claim> { new Claim("scope", scope) }));

            ctx.Request.Method = "GET";
            return ctx;
        }
    }
}
