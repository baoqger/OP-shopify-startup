using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc.Filters;
using Newtonsoft.Json;
using ShopifySharp;

namespace AuntieDot.Attributes
{
    /// <summary>
    /// A filter that will catch all permission exceptions thrown by ShopifySharp and prompt the user to accept the latest permissions.
    /// </summary>
    public class CatchPermissionExceptionsAttribute : ExceptionFilterAttribute
    {
        public override void OnException(ExceptionContext context)
        {

            if (context.ExceptionHandled || !context.HttpContext.Response.Body.CanWrite)
            {
                return;
            }

            if (context.Exception is ShopifyException ex && (int) ex.HttpStatusCode == 403)
            {
                var logger = (ILogger) context.HttpContext.RequestServices.GetService(typeof(ILogger<CatchPermissionExceptionsAttribute>));

                logger.LogWarning("User does not have permission to access Shopify resource, redirecting to login page");

                context.Result = new Microsoft.AspNetCore.Mvc.RedirectResult("/auth/login");
                context.ExceptionHandled = true;
            }
        }
    }
}
