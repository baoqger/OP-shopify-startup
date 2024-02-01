using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AuntieDot.Models;
using AuntieDot.Extensions;
using ShopifySharp;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace AuntieDot.Attributes
{
    public class ValidateShopifyWebhookAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var rawBody = await context.HttpContext.Request.ReadRawBodyAsync();
            var secrets = (ISecrets) context.HttpContext.RequestServices.GetService(typeof(ISecrets));
            var isAuthentic = AuthorizationService.IsAuthenticWebhook(context.HttpContext.Request.Headers, rawBody, secrets.ShopifySecretKey);

            if (isAuthentic)
            {
                await next();
            }
            else
            {
                // Request did not pass validation. Return a JSON error message 
                context.HttpContext.Response.ContentType = "application/json";

                var body = JsonConvert.SerializeObject(new
                {
                    message = "Webhook did not pass validation result.",
                    ok = false
                });

                using (var buffer = new MemoryStream(Encoding.UTF8.GetBytes(body)))
                {
                    context.HttpContext.Response.StatusCode = 401;
                    await buffer.CopyToAsync(context.HttpContext.Response.Body);
                }
            }
        }
    }
}
