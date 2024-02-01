using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using AuntieDot.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Text.Encodings.Web;

namespace AuntieDot.Middleware
{
    public class CookieSessionAuthenticationHandler : CookieAuthenticationHandler
    {
        public CookieSessionAuthenticationHandler(
            IOptionsMonitor<CookieAuthenticationOptions> options, 
            ILoggerFactory logger, 
            UrlEncoder encoder, 
            ISystemClock clock
        ) : base(options, logger, encoder, clock)
        {
            
        }
        
        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var result = await base.HandleAuthenticateAsync();

            if (!result.Succeeded)
            {
                return result;
            }
            
            // Change the principal so it uses a CookieSession identity
            var session = new CookieSession(result.Principal.Claims);
            var principal = new ClaimsPrincipal(session);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            
            return AuthenticateResult.Success(ticket);
        }
    }
}
