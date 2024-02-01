using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AuntieDot.Models;
using ShopifySharp;

namespace AuntieDot.Attributes
{
    public class ValidateShopifyRequestAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var secrets = (ISecrets) context.HttpContext.RequestServices.GetService(typeof(ISecrets));
            var isAuthentic = AuthorizationService.IsAuthenticRequest(context.HttpContext.Request.Query, secrets.ShopifySecretKey);

            if (isAuthentic)
            {
                await next();
            }
            else
            {
                context.Result = new ForbidResult();
            }
        }
    }
}