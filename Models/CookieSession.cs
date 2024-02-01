using System.Security.Claims;
using System.Collections.Generic;
using System.Linq;
using System;
using AuntieDot.Middleware;

namespace AuntieDot.Models
{
    public class CookieSession : ClaimsIdentity
    {
        public CookieSession(UserAccount userAccount = null)
        {
            if (userAccount != null)
            {
                Name = userAccount.ShopifyShopDomain;
                UserId = userAccount.Id;
                ShopifyShopDomain = userAccount.ShopifyShopDomain;
            }
        }

        public CookieSession(IEnumerable<Claim> userClaims)
        {
            T Find<T>(string claimName, Func<string, T> valueConverter)
            {
                var claim = userClaims.FirstOrDefault(claim => claim.Type == claimName);

                if (claim == null)
                {
                    throw new NullReferenceException($"Session claim {claimName} was not found, unable to parse user principal to CookieSession.");
                }

                return valueConverter(claim.Value);
            }
            
            Name = Find("Name", str => str);
            UserId = Find("UserId", int.Parse);
            ShopifyShopDomain = Find("ShopifyShopDomain", str => str);
        }

        public override string AuthenticationType => CookieSessionDefaults.AuthenticationScheme;

        public override bool IsAuthenticated => true;

        public override string Name { get; }
        
        public int UserId { get; set; }

        public string ShopifyShopDomain { get; set; }
    }
}
