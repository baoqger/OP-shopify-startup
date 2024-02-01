using System;
using System.Threading.Tasks;
using AuntieDot.Attributes;
using AuntieDot.Data;
using AuntieDot.Extensions;
using AuntieDot.Models;
using Microsoft.AspNetCore.Authentication;
using ShopifySharp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using ShopifySharp.Filters;

namespace AuntieDot.Controllers
{
    public class ShopifyController : Controller
    {
        public ShopifyController(DataContext dataContext, ISecrets secrets, IApplicationUrls appUrls)
        {
            _dataContext = dataContext;
            _secrets = secrets;
            _appUrls = appUrls;
        }
        
        private readonly DataContext _dataContext;
        private readonly ISecrets _secrets;
        private readonly IApplicationUrls _appUrls;

        [HttpGet]
        public async Task<ActionResult> Handshake([FromQuery] string shop)
        {
            if (string.IsNullOrEmpty(shop))
            {
                return Problem("Request is missing shop querystring parameter.", statusCode: 422);
            }
            
            // Check if the user is already logged in
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                // Check if the user is logged in as the same shop they're attempting to use
                var user = await _dataContext.GetUserFromSessionAsync(HttpContext.User);
                
                // If the shop domains match, the user is already logged in and can be sent to the home page
                if (user.ShopifyShopDomain == shop)
                {
                    return RedirectToAction("Index", "Orders");
                }
                
                // If the shop domains don't match, the user likely owns two or more Shopify shops and they're trying
                // to log in to a separate one. Log them out and let them be redirected to the login page.
                await HttpContext.SignOutAsync();
            }

            // The user has either not yet installed the app, is not logged in, or was logged in to a different shop.
            // Send them to the login page
            return RedirectToAction("Login", "Auth");
        }

        [HttpGet, ValidateShopifyRequest]
        public async Task<ActionResult> AuthResult([FromQuery] string shop, [FromQuery] string code, [FromQuery] string state)
        {
            // Check to make sure the state token has not already been used
            var stateToken = await _dataContext.States.FirstOrDefaultAsync(t => t.Token == state);

            if (stateToken == null)
            {
                // This token has already been used. The user must go through the OAuth process again
                return RedirectToAction("HandleLogin", "Auth");
            }
            
            // Delete the token so it can't be used again
            _dataContext.States.Remove(stateToken);
            await _dataContext.SaveChangesAsync();
            
            // Exchange the temporary code for a permanent access token
            string accessToken;

            try
            {
                accessToken = await AuthorizationService.Authorize(code, shop, _secrets.ShopifyApiKey, _secrets.ShopifySecretKey);
            }
            catch (ShopifyException ex) when ((int) ex.HttpStatusCode == 400)
            {
                // The code has already been used or has expired. The user must go through the OAuth process again. 
                return View("/Views/Auth/Login.cshtml", new LoginViewModel
                {
                    Error = "The temporary Shopify install/login token has expired. Please try again.",
                    ShopDomain = shop
                });
            }
            
            // Get the user's shop data so we can use the shop id 
            var shopData = await new ShopService(shop, accessToken).GetAsync();

            // Check to see if a user account already exists for this shop and needs to be updated, or if it needs to be created
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.ShopifyShopDomain == shop);

            if (user == null)
            {
                // Create the user's account
                user = new UserAccount
                {
                    ShopifyAccessToken = accessToken,
                    ShopifyShopDomain = shop,
                    ShopifyShopId = shopData.Id.Value
                };

                _dataContext.Add(user);
            }
            else
            {
                // Update the user's account
                user.ShopifyAccessToken = accessToken;
                user.ShopifyShopDomain = shop;
                user.ShopifyShopId = shopData.Id.Value;
            }

            await _dataContext.SaveChangesAsync();
            
            // Sign the new user in
            await HttpContext.SignInAsync(user);

            // Check if the AppUninstalled webhook already exists
            var service = new WebhookService(shop, accessToken);
            var topic = "app/uninstalled";
            var existingHooks = await service.ListAsync(new WebhookFilter
            {
                Topic = topic
            });

            if (!existingHooks.Items.Any())
            {
                // Create the AppUninstalled webhook
                await service.CreateAsync(new Webhook
                {
                    Address = _appUrls.AppUninstalledWebhookUrl,
                    Topic = topic
                });
            }

            // Check if the user needs to activate their subscription charge
            if (!user.ShopifyChargeId.HasValue)
            {
                return RedirectToAction("Start", "Subscription");
            }
            
            // User is subscribed, send them to the home page
            return RedirectToAction("Index", "Home");
        }
    }
}
