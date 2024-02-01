using AuntieDot.Models;
using AuntieDot.Middleware;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IO;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace AuntieDot.Extensions
{
    public static class HttpContextExtensions
    {
        /// <summary>
        /// Signs the user in, making their session available to future requests. 
        /// </summary>
        public static async Task SignInAsync(this HttpContext ctx, CookieSession session)
        {
            session.AddClaims(new List<Claim>
            {
                new Claim("Name", session.Name, ClaimValueTypes.String),
                new Claim("UserId", session.UserId.ToString(), ClaimValueTypes.Integer32),
                new Claim("ShopifyShopDomain", session.ShopifyShopDomain, ClaimValueTypes.String)
            });
            var scheme = CookieSessionDefaults.AuthenticationScheme;

            await ctx.SignInAsync(scheme, new ClaimsPrincipal(session));
        }

        /// <summary>
        /// Signs the user in, making their session available to future requests. 
        /// </summary>
        public static async Task SignInAsync(this HttpContext ctx, UserAccount userAccount) => 
            await SignInAsync(ctx, new CookieSession(userAccount));

        /// <summary>
        /// Gets the user's shop domain from their session.
        /// </summary>
        public static string GetUserShopDomain(this ClaimsPrincipal userPrincipal)
        {
            foreach (var identity in userPrincipal.Identities)
            {
                switch (identity)
                {
                    case CookieSession cookie:
                        return cookie.ShopifyShopDomain;
                }
            }

            throw new NullReferenceException($"Unable to find suitable identity type to determine user's shop domain. Is the user authenticated?");
        }

        public static CookieSession GetUserSession(this ClaimsPrincipal userPrincipal) {
            if (!userPrincipal.Identity.IsAuthenticated) {
                throw new Exception("User is not authenticated, cannot get user session.");
            }
            // an inline function that looks for properties on the user principal 
            // and convert them to a desired type 
            T Find<T>(string claimName, Func<string, T> valueConverter) {
                var claim = userPrincipal.Claims.FirstOrDefault(claim => claim.Type == claimName);
                if (claim == null)
                {
                    throw new NullReferenceException($"Session claim {claimName} wasnot found.");
                }
                return valueConverter(claim.Value);
            }
            var session = new CookieSession {
                UserId = Find("UserId", int.Parse),
                ShopifyShopDomain = Find("ShopifyShopDomain", str => str),
                // Name = Find("Name", str => str)
            };
            return session;
        }
    }
}
