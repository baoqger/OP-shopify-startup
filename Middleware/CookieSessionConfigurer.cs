using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace AuntieDot.Middleware
{
    public static class CookieSessionAuthenticationConfigurer
    {
        /// <summary>
        /// Adds cookie authentication to <see cref="AuthenticationBuilder"/> using the specified scheme.
        /// <para>
        /// Cookie authentication uses a HTTP cookie persisted in the client to perform authentication.
        /// </para>
        /// </summary>
        /// <param name="builder">The <see cref="AuthenticationBuilder"/>.</param>
        /// <param name="configureOptions">A delegate to configure <see cref="CookieAuthenticationOptions"/>.</param>
        /// <returns>A reference to <paramref name="builder"/> after the operation has completed.</returns>
        public static AuthenticationBuilder AddCookieSession(this AuthenticationBuilder builder, Action<CookieAuthenticationOptions> configureOptions)
        {
            var authenticationScheme = CookieSessionDefaults.AuthenticationScheme;
            
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureCookieAuthenticationOptions>());
            builder.Services.AddOptions<CookieAuthenticationOptions>(authenticationScheme).Validate(o => o.Cookie.Expiration == null, "Cookie.Expiration is ignored, use ExpireTimeSpan instead.");
            
            return builder.AddScheme<CookieAuthenticationOptions, CookieSessionAuthenticationHandler>(authenticationScheme, configureOptions);
        }
    }
}
