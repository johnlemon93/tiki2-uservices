using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Threading.Tasks;
using LibOwin;
using Microsoft.IdentityModel.Tokens;
using Nancy.Bootstrapper;

namespace Uservice.Auth
{
    using Microsoft.IdentityModel.Logging;
    using Nancy;
    using Nancy.Owin;
    using System.Security.Claims;
    using System.Text;
    using AppFunc = Func<IDictionary<string, object>, Task>;

    public class Authorization
    {
        public static AppFunc Middleware(AppFunc next, string requiredScope)
        {
            return env =>
            {
                var ctx = new OwinContext(env);
                var principal = ctx.Request.User;

                // calls next in the pipeline only if the request has the required scope
                if (principal != null && principal.HasClaim("scope", requiredScope))
                {
                    return next(env);
                }

                // otherwise gives a 403 Forbidden response
                ctx.Response.StatusCode = 403;
                return Task.FromResult(0);
            };
        }
    }

    /// <summary>
    /// TODO: implement passing secretKey/signingKey to the middle ware
    /// </summary>
    public class IdToken
    {
        public static AppFunc Middleware(AppFunc next)
        {
            const string userIdHeaderKey = "uservice-end-user";

            return env =>
            {
                var ctx = new OwinContext(env);

                // checks for the header that should contain the end user's identity
                if (ctx.Request.Headers.ContainsKey(userIdHeaderKey))
                {
                    var keyBytes = Encoding.UTF8.GetBytes("secret");
                    Array.Resize(ref keyBytes, 64);

                    var tokenValidationParameters = new TokenValidationParameters
                    {
                        // The signing key must match!
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                        ValidateAudience = false,
                        //ValidAudience = "www.example.com",
                        ValidateIssuer = false,
                        //ValidIssuer = "",
                        //ValidIssuers = new [] {""}
                    };
                    IdentityModelEventSource.ShowPII = true;

                    // reads and validates the end user's identity
                    var tokenHandler = new JwtSecurityTokenHandler();
                    var userPrincipal =
                      tokenHandler.ValidateToken(ctx.Request.Headers[userIdHeaderKey],
                                                 tokenValidationParameters, out SecurityToken token);

                    // creates a user object based on the calims in the end user's identity, and adds it to the OWIN context
                    ctx.Set("pos-end-user", userPrincipal);
                }

                return next(env);
            };
        }
    }

    /// <summary>
    /// Reads the user object back out of the OWIN context and pass it on to NancyContext, 
    /// so Nancy modules know about the user to perform authorization
    /// <para>
    /// Note: Nancy will automatically pick up this implementation (though in NuGet packages)
    /// just like bootstrappers and modules at application startup time and hook it into the request pipeline
    /// </para>
    /// </summary>
    public class SetUser : IRequestStartup
    {
        /// <summary>
        /// Called on each request
        /// </summary>
        public void Initialize(IPipelines pipelines, NancyContext context)
        {
            const string userIdKey = "pos-end-user";
            var owinEnv = context.GetOwinEnvironment();

            if (owinEnv.ContainsKey(userIdKey))
            {
                context.CurrentUser = owinEnv[userIdKey] as ClaimsPrincipal;
            }
        }
    }
}
