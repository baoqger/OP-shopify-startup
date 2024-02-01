using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AuntieDot.Extensions;
using System.Linq;
using Microsoft.Extensions.Caching.Memory;
using System;
using AuntieDot.Data;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic;
using AuntieDot.Middleware;
using AuntieDot.Infrastructure;
using AuntieDot.Models;

namespace AuntieDot.Attributes
{
    public class AuthorizeWithActiveSubscriptionAttribute : AuthorizeAttribute, IAsyncAuthorizationFilter
    {
        public AuthorizeWithActiveSubscriptionAttribute()
        {
            var schemes = new List<string>
            {
                CookieSessionDefaults.AuthenticationScheme
            };

            AuthenticationSchemes = string.Join(",", schemes);
        }

        public async Task OnAuthorizationAsync(AuthorizationFilterContext ctx)
        {
            // Check if the user is authenticated first
            if (!ctx.HttpContext.User.Identity.IsAuthenticated)
            {
                // The base class will handle basic authentication
                return;
            }

            var anonAttributeType = typeof(AllowAnonymousAttribute);
            var allowAnonymous = 
                ctx.Filters.Any(f => f.GetType() == anonAttributeType)
                || ctx.ActionDescriptor.EndpointMetadata.Any(f => f.GetType() == anonAttributeType)
                || ctx.ActionDescriptor.FilterDescriptors.Any(f => f.Filter.GetType() == anonAttributeType);

            if (allowAnonymous)
            {
                // The route explicitly allows anonymous connections, so we don't need to check authentication
                return;
            }

            var shopDomain = ctx.HttpContext.User.GetUserShopDomain();
            var services = ctx.HttpContext.RequestServices;
            var cache = (IMemoryCache) services.GetService(typeof(IMemoryCache));
            var dataContext = (DataContext) services.GetService(typeof(DataContext));

            // Grab the user's full account from the cache
            var cachedUser = await cache.GetOrCreateAsync(shopDomain, async x =>
            {
                var userAccount = await dataContext.Users
                    .FirstOrDefaultAsync(u => u.ShopifyShopDomain == shopDomain);

                if (userAccount != null)
                {
                    x.SetValue(userAccount);
                    x.SetAbsoluteExpiration(TimeSpan.FromSeconds(60));
                }
                else
                {
                    // No account was found, so we don't cache this result
                    x.SetAbsoluteExpiration(TimeSpan.FromSeconds(1));
                }

                return userAccount;
            });

            // Check that this user is authenticated. If not, redirect them to the subscription page.
            if (cachedUser?.HasActiveSubscription != true)
            {
                // Redirect the user to /subscription/start where they can start a subscription
                ctx.Result = new RedirectResult("/subscription/start");
            }
        }
    }
}
