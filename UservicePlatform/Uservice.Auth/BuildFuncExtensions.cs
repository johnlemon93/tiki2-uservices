using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Uservice.Auth
{
    using BuildFunc = Action<Func<
        Func<IDictionary<string, object>, Task>,
        Func<IDictionary<string, object>, Task>>>;

    public static class BuildFuncExtensions
    {
        public static BuildFunc UseAuthPlatform(this BuildFunc buildFunc, string requiredScope)
        {
            buildFunc(next => Authorization.Middleware(next, requiredScope));
            buildFunc(next => IdToken.Middleware(next));
            return buildFunc;
        }
    }
}
