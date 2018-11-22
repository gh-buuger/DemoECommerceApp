using DemoECommerceApp.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace DemoECommerceApp.Security.Authentication
{
    public class BasicAuthenticationHandler : AuthenticationHandler<BasicAuthenticationOptions>
    {
        private readonly FlixOneStoreContext _context;
        private const string AuthorizationHeaderName = "Authorization";
        private const string BasicSchemeName = "Basic";

        public BasicAuthenticationHandler(
            IOptionsMonitor<BasicAuthenticationOptions> options, 
            ILoggerFactory logger, 
            UrlEncoder encoder, 
            ISystemClock clock,
            FlixOneStoreContext context) : base(options, logger, encoder, clock)
        {
            _context = context;
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            //1. Verify if AuthorizationHeaderName presented in the Header.
            if (!Request.Headers.ContainsKey(AuthorizationHeaderName))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            //2. Verify if Header is valid.
            if (!AuthenticationHeaderValue.TryParse(Request.Headers[AuthorizationHeaderName], out AuthenticationHeaderValue headerValue))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            //3. Verify if scheme name is Basic.
            if (!BasicSchemeName.Equals(headerValue.Scheme, StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Basic Authentication Schema."));
            }

            //4. Fetch Email and Password from Header.
            var headerValueParamInBytes = Convert.FromBase64String(headerValue.Parameter);
            var emailAndPassword = Encoding.UTF8.GetString(headerValueParamInBytes);
            var parts = emailAndPassword.Split(":");
            if (parts.Length != 2)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Basic Authentication Header."));
            }
            var email = parts[0];
            var password = parts[1];

            //5. Validate Email and Password.
            var customer = _context.Customers.SingleOrDefault(x => x.Email == email && x.Password == password);

            if (customer == null)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid Email or Password."));
            }

            //6. Return ticket.
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, email),
                new Claim(ClaimTypes.NameIdentifier, customer.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
