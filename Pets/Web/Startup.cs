using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Owin;
using Owin;

[assembly: OwinStartup(typeof(Pets.Startup))]

namespace Pets
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }

        public void ConfigureOAuth(IAppBuilder app)
        {
            // app.CreatePerOwinContext(Social123.AuthService.Plumbing.AuthContext.Create);

            OAuthAuthorizationServerOptions OAuthServerOptions = new OAuthAuthorizationServerOptions()
            {
                AllowInsecureHttp = true,
                TokenEndpointPath = new PathString("/token"),
                AccessTokenExpireTimeSpan = TimeSpan.FromHours(12),
                Provider = new SimpleAuthorizationServerProvider()
                // RefreshTokenProvider = new SimpleRefreshTokenProvider()//,
                // OnValidateClientAuthentication = ValidateClientAuthentication,

            };

            // app.use
            // Token Generation
            app.UseOAuthAuthorizationServer(OAuthServerOptions);
            app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

        }
    }
}
