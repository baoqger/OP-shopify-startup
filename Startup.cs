using AuntieDot.Data;
using AuntieDot.Helpers;
using AuntieDot.Middleware;
using AuntieDot.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;

namespace AuntieDot
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        string GetSqlConnectionString()
        {
            var connStr = new SqlConnectionStringBuilder(Configuration.GetConnectionString("DefaultConnection"))
            {
                Password = Configuration.GetValue<string>("SQL_PASSWORD"),
                Authentication = SqlAuthenticationMethod.SqlPassword
            };

            return connStr.ToString();
        }

        private static void ConfigureCookieAuthentication(CookieAuthenticationOptions options)
        {
            options.Cookie.HttpOnly = true;
            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromDays(1);
            options.LogoutPath = "/Auth/Logout";
            options.LoginPath = "/Auth/Login";
            options.AccessDeniedPath = "/Auth/Login";
            options.Cookie.SameSite = SameSiteMode.None;
            
            options.Validate();
        }

        /// <summary>
        /// Checks if the user agent does not support SameSiteMode.None.
        /// </summary>
        /// <remarks>
        /// Source: https://devblogs.microsoft.com/aspnet/upcoming-samesite-cookie-changes-in-asp-net-and-asp-net-core/
        /// </remarks>
        private bool DisallowsSameSiteNone(string userAgent)
        {
            if (string.IsNullOrEmpty(userAgent))
            {
                return false;
            }

            // Cover all iOS based browsers here. This includes:
            // - Safari on iOS 12 for iPhone, iPod Touch, iPad
            // - WkWebview on iOS 12 for iPhone, iPod Touch, iPad
            // - Chrome on iOS 12 for iPhone, iPod Touch, iPad
            // All of which are broken by SameSite=None, because they use the iOS networking stack
            if (userAgent.Contains("CPU iPhone OS 12") || userAgent.Contains("iPad; CPU OS 12"))
            {
                return true;
            }

            // Cover Mac OS X based browsers that use the Mac OS networking stack. This includes:
            // - Safari on Mac OS X.
            // This does not include:
            // - Chrome on Mac OS X
            // Because they do not use the Mac OS networking stack.
            if (userAgent.Contains("Macintosh; Intel Mac OS X 10_14") && 
                userAgent.Contains("Version/") && userAgent.Contains("Safari"))
            {
                return true;
            }

            // Cover Chrome 50-69, because some versions are broken by SameSite=None, 
            // and none in this range require it.
            // Note: this covers some pre-Chromium Edge versions, 
            // but pre-Chromium Edge does not require SameSite=None.
            if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
            {
                return true;
            }

            return false;
        }
        
        private void CheckSameSite(HttpContext context, CookieOptions options)
        {
            if (options.SameSite == SameSiteMode.None)
            {
                var userAgent = context.Request.Headers["User-Agent"].ToString();

                if (DisallowsSameSiteNone(userAgent))
                {
                    options.SameSite = SameSiteMode.Unspecified;
                }
            }
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // services.AddAuthentication(CookieSessionDefaults.AuthenticationScheme).AddCookieSession(ConfigureCookieAuthentication);
            services.AddAuthentication(CookieSessionDefaults.AuthenticationScheme).AddCookie(CookieSessionDefaults.AuthenticationScheme, ConfigureCookieAuthentication);
            services.AddControllersWithViews();
            // Add the database context which injects the context into each controller's constructor
            services.AddDbContext<DataContext>(options => options.UseSqlServer(GetSqlConnectionString()));
            // Add ISecrets so classes can use the Shopify secret/api keys
            services.AddSingleton<ISecrets, Secrets>();
            // Add URLs
            services.AddSingleton<IApplicationUrls, ApplicationUrls>();
            // Add the OAuth helper, must be scoped because it uses the DataContext
            services.AddScoped<IOauthHelper, OauthHelper>();
            // Add the memory cache
            services.AddSingleton<IMemoryCache, MemoryCache>();
            // Configure Shopify-specific cookie and header options for loading the app in an embedded admin page
            services.AddAntiforgery(c =>
            {
                // All embedded apps are loaded in an iframe. The server must not send the X-Frame-Options: Deny header
                c.SuppressXFrameOptionsHeader = true;
                c.Cookie.SameSite = SameSiteMode.None;
            });
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Unspecified;
                options.Secure = CookieSecurePolicy.Always;
                options.OnAppendCookie = cookieContext => CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
                options.OnDeleteCookie = cookieContext => CheckSameSite(cookieContext.Context, cookieContext.CookieOptions);
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.UseHttpsRedirection();
            }
            app.UseStatusCodePages();
            app.UseStaticFiles();
            app.UseAuthentication();
            app.UseRouting();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
