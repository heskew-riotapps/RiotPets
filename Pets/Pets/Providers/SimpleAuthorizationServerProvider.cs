using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Owin.Security.OAuth;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Security.Claims;
using Social123.AuthService.Objects;
using Owin;
using Microsoft.AspNet.Identity.Owin;
using Social123.DataTools.BusinessEntities.Accounts;
using Social123.Services.AccountService;
using Social123.DataTools.BusinessEntities.Auth;
using Social123.Services.Auth;
using Microsoft.Owin.Security;
using log4net;


namespace Pets.Web.Providers
{
    public class SimpleAuthorizationServerProvider : OAuthAuthorizationServerProvider
    {
        private static readonly ILog logger = LogManager.GetLogger(typeof(SimpleAuthorizationServerProvider));
        public override Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        {
            //               grant_type=client_credentials&
            //client_id=4D78F2A8-8C62-49B1-83A2-799F80329453&
            //client_secret=6F816541-62BC-4F1C-BB2A-487CFAC21879
            string clientId = string.Empty;
            string clientSecret = string.Empty;
            ApiClient client = null;

            if (!context.TryGetBasicCredentials(out clientId, out clientSecret))
            {
                context.TryGetFormCredentials(out clientId, out clientSecret);
            }

            if (context.ClientId == null)
            {
                //Remove the comments from the below line context.SetError, and invalidate context 
                //if you want to force sending clientId/secrects once obtain access tokens. 
                context.Validated();
                //context.SetError("invalid_clientId", "ClientId should be sent.");
                return Task.FromResult<object>(null);
            }

            using (Social123.Services.Auth.AuthService _svc = new Social123.Services.Auth.AuthService())
            {
                client = _svc.FindClient(new Guid(context.ClientId));
            }

            if (client == null)
            {
                context.SetError("invalid_clientId", string.Format("Client '{0}' is not registered in the system.", context.ClientId));
                logger.Debug(string.Format("invalid_clientId, Client '{0}': Not registered in the system.", context.ClientId));
                return Task.FromResult<object>(null);
            }

            if (client.ApplicationTypeId == AuthEnums.eApplicationType.NativeConfidential)
            {
                if (string.IsNullOrWhiteSpace(clientSecret))
                {
                    context.SetError("invalid_clientId", "Client secret should be sent.");
                    logger.Debug(string.Format("invalid_clientId, Client '{0}': Client secret should be sent.", context.ClientId));
                    return Task.FromResult<object>(null);
                }
                else
                {
                    if (client.Secret != clientSecret) //Social123.DataTools.Common.Utilities.GetHash(clientSecret))
                    {
                        context.SetError("invalid_clientId", "Client secret is invalid.");
                        logger.Debug(string.Format("invalid_clientId, Client '{0}': Client secret is invalid. stored: {1}, incoming: {2}", context.ClientId, clientSecret, client.Secret));
                        return Task.FromResult<object>(null);
                    }
                }
            }

            if (client.StatusId != AuthEnums.eApiClientStatus.Active)
            {
                context.SetError("invalid_clientId", "Client is inactive.");
                logger.Debug(string.Format("invalid_clientId, Client '{0}': Client is inactive.", context.ClientId));
                return Task.FromResult<object>(null);
            }

            context.OwinContext.Set<string>("as:clientAllowedOrigin", client.AllowedOrigin);
            context.OwinContext.Set<string>("as:clientRefreshTokenLifeTime", client.RefreshTokenLifeTime.ToString());

            context.Validated();
            return Task.FromResult<object>(null);
        }

        public override Task GrantRefreshToken(OAuthGrantRefreshTokenContext context)
        {
            var originalClient = context.Ticket.Properties.Dictionary["as:client_id"];
            var currentClient = context.ClientId;

            if (originalClient != currentClient)
            {
                context.SetError("invalid_clientId", "Refresh token is issued to a different clientId.");
                return Task.FromResult<object>(null);
            }

            // Change auth ticket for refresh token requests
            var newIdentity = new ClaimsIdentity(context.Ticket.Identity);
            newIdentity.AddClaim(new Claim("newClaim", "newValue"));

            var newTicket = new AuthenticationTicket(newIdentity, context.Ticket.Properties);
            context.Validated(newTicket);

            return Task.FromResult<object>(null);
        }

        //////// This validates the identity based on the issuer of the claim.
        //////// The issuer is set in the API endpoint that logs the user in
        //////public override Task ValidateIdentity(OAuthValidateIdentityContext context)
        //////{
        //////    var claims = context.Ticket.Identity.Claims;
        //////    if (claims.Count() == 0 || claims.Any(claim => claim.Issuer != "Facebook" && claim.Issuer != "LOCAL_AUTHORITY"))
        //////        context.Rejected();
        //////    return Task.FromResult<object>(null);
        //////}
        //private Task ValidateClientAuthentication(OAuthValidateClientAuthenticationContext context)
        //{
        //    string clientId;
        //    string clientSecret;
        //    if (context.TryGetBasicCredentials(out clientId, out clientSecret) ||
        //        context.TryGetFormCredentials(out clientId, out clientSecret))
        //    {
        //        if (clientId == Clients.Client1.Id && clientSecret == Clients.Client1.Secret)
        //        {
        //            context.Validated();
        //        }
        //        else if (clientId == Clients.Client2.Id && clientSecret == Clients.Client2.Secret)
        //        {
        //            context.Validated();
        //        }
        //    }
        //    return Task.FromResult(0);
        //}

        public override async Task GrantClientCredentials(OAuthGrantClientCredentialsContext context)
        {
            //http://bitoftech.net/2014/07/16/enable-oauth-refresh-tokens-angularjs-app-using-asp-net-web-api-2-owin/

            ApiClient client = null;


            if (context.ClientId == null)
            {
                context.SetError("invalid_grant", "invalid client id.");
                return;
            }

            using (Social123.Services.Auth.AuthService _svc = new Social123.Services.Auth.AuthService())
            {
                client = _svc.FindClient(new Guid(context.ClientId));
            }

            if (client == null)
            {
                context.SetError("invalid_grant", "invalid client id.  not found.");
                return;
            }
            ApplicationUser user = null;

            //fetch the API user for this account
            using (AuthRepository _repo = new AuthRepository(HttpContext.Current.GetOwinContext().GetUserManager<Social123.AuthService.Plumbing.ApplicationUserManager>()))
            {
                user = await _repo.GetApiUser(client.AccountId);
                if (user == null)
                {
                    context.SetError("invalid_grant", "api user not found.");
                    return;
                }

            }


            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            // identity.AddClaim(new Claim("sub", context.UserName));
            //identity.AddClaim(new Claim("_u", user.FirstName + " " + user.LastName ));
            identity.AddClaim(new Claim(ClaimTypes.Role, Social123.AuthService.Plumbing.Constants.ApiUserRole));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.FirstName));

            var props = new AuthenticationProperties(new Dictionary<string, string>
                {
                    { 
                        "as:client_id", (context.ClientId == null) ? string.Empty : context.ClientId
                    },
                    { 
                        "userName", user.UserName
                    },
                    { 
                        "userId", user.Id.ToString()
                    }
                });

            var ticket = new AuthenticationTicket(identity, props);
            context.Validated(ticket);
            //  context.Validated(identity);
        }

        public override async Task GrantResourceOwnerCredentials(OAuthGrantResourceOwnerCredentialsContext context)
        {

            context.OwinContext.Response.Headers.Add("Access-Control-Allow-Origin", new[] { "*" });

            ApplicationUser user = null;
            var role = string.Empty;

            using (AuthRepository _repo = new AuthRepository(HttpContext.Current.GetOwinContext().GetUserManager<Social123.AuthService.Plumbing.ApplicationUserManager>()))
            {
                user = await _repo.FindUser(context.UserName, context.Password);
                if (user != null && user.Roles != null && user.Roles.Count > 0)
                {
                    role = _repo.GetRole(user.Roles.First().RoleId).Name;
                }

            }
            if (user == null)
            {
                context.SetError("invalid_grant", Social123.Resources.Strings.Exceptions.LoginFailed);
                return;
            }
            if (user.StatusId == ApplicationUser.eStatus.PendingActivation)
            {
                var _accountMgr = new Social123.Services.AccountService.AccountManager();
                if (_accountMgr.IsAccountEloquaEnabled(user.AccountId ?? Guid.Empty))
                {
                    context.SetError("invalid_grant", Social123.Resources.Strings.Exceptions.LoginUserNotActiveEloqua);
                }
                else
                {
                    context.SetError("invalid_grant", Social123.Resources.Strings.Exceptions.LoginUserNotActive);
                }
                return;
            }
            if (user.StatusId != ApplicationUser.eStatus.Active)
            {
                context.SetError("invalid_grant", Social123.Resources.Strings.Exceptions.LoginUserNotActive);
                return;
            }

            if (user.AccountId != null) // this can be refactored after all users have account id
            {
                Account account = null;
                using (var _accountMgr = new AccountManager(System.Configuration.ConfigurationManager.ConnectionStrings["S123Admin"].ConnectionString))
                {
                    account = await _accountMgr.GetAccount((Guid)user.AccountId);
                }

                if (account != null && (account.StatusId == AccountEnums.eAccountStatus.Deleted || account.StatusId == AccountEnums.eAccountStatus.Suspended))
                {
                    context.SetError("invalid_grant", Social123.Resources.Strings.Exceptions.LoginAccountNotActive);
                    return;
                }
                else
                {
                    //for now, if account is in trial status, simply set all of the account's user to trial status (only in httpcontext/identity)
                    if (account.StatusId == AccountEnums.eAccountStatus.Trial)
                    {
                        role = Social123.AuthService.Plumbing.Constants.TrialRole;
                    }
                }
            }


            var identity = new ClaimsIdentity(context.Options.AuthenticationType);
            // identity.AddClaim(new Claim("sub", context.UserName));
            //identity.AddClaim(new Claim("_u", user.FirstName + " " + user.LastName ));
            identity.AddClaim(new Claim(ClaimTypes.Role, role));
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, user.Id));
            identity.AddClaim(new Claim(ClaimTypes.Name, user.FirstName));
            // identity.AddClaim(new Claim(ClaimTypes., user.FirstName));
            //   identity.AddClaim(new Claim(ClaimTypes.))

            // identity.AddClaim(new Claim("role", role));

            var props = new AuthenticationProperties(new Dictionary<string, string>
                {
                    { 
                        "as:client_id", (context.ClientId == null) ? string.Empty : context.ClientId
                    },
                    { 
                        "userName", user.UserName
                    },
                    { 
                        "userId", user.Id.ToString()
                    }
                });

            var ticket = new AuthenticationTicket(identity, props);
            context.Validated(ticket);

            // context.Validated(identity);

        }
    }
}