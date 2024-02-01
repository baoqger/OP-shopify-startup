using ShopifySharp;
using System;
using System.Threading.Tasks;
using AuntieDot.Data;
using AuntieDot.Models;
using AuntieDot.Extensions;
using AuntieDot.Attributes;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuntieDot.Controllers
{
    [ValidateShopifyWebhook]
    public class WebhooksController : Controller
    {
        public WebhooksController(ISecrets secrets, DataContext userContext, ILogger<WebhooksController> logger)
        {
            _secrets = secrets;
            _dataContext = userContext;
            _logger = logger;
        }
        
        private readonly ISecrets _secrets;
        private readonly DataContext _dataContext;
        private readonly ILogger<WebhooksController> _logger;

        [HttpPost]
        public async Task<StatusCodeResult> AppUninstalled([FromQuery] string shop)
        {
            if (string.IsNullOrWhiteSpace(shop))
            {
                _logger.LogWarning("Received AppUninstalled webhook but the shop value was null or empty.");
                return Ok();
            }

            // Pull in the user
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.ShopifyShopDomain == shop);

            if (user == null)
            {
                _logger.LogWarning($"Received AppUninstalled webhook for shop {shop}, but a user with that shop value could not be found.");
                // User does not exist or may have already been deleted
                return Ok();
            }
            
            _logger.LogInformation($"Received AppUninstalled webhook for shop {shop}");

            //Delete their subscription charge and Shopify details
            user.ShopifyChargeId = null;
            user.ShopifyAccessToken = null;
            user.ShopifyShopDomain = null;
            user.BillingOn = null;

            // Save changes to the database
            await _dataContext.SaveChangesAsync();

            return Ok();
        }

        [HttpPost]
        public async Task<IActionResult> GdprCustomerDataRequest()
        {
            var data = await Request.DeserializeBodyAsync<ShopifySharp.CustomerDataRequestWebhook>();
            
            // According to Shopify's GDPR guidelines, the developer (us) is responsible for sending the requested data **to the store owner**. 
            var requestedOrders = string.Join(", ", data.OrdersRequested ?? Enumerable.Empty<long>());
            var message = $"Customer {data.Customer.Id} has requested their data via shop {data.ShopId} ({data.ShopDomain}). Orders requested: {requestedOrders}. Customer email: {data.Customer.Email}; Customer phone: {data.Customer.Phone}.";
            
            _logger.LogCritical(message);
            
            return Ok("GDPR customer data request received.");
        }

        [HttpPost]
        public async Task<IActionResult> GdprCustomerRedacted()
        {
            var data = await Request.DeserializeBodyAsync<ShopifySharp.CustomerRedactedWebhook>();
            
            // Log the redaction
            var message = $"Customer {data.Customer.Id} for shop {data.ShopId} ({data.ShopDomain} has been redacted. Customer email: {data.Customer.Email}; Customer phone: {data.Customer.Phone}.";
            
            _logger.LogWarning(message);

            // This app does not currently log shop customer data, nothing to do here
            
            return Ok("GDPR customer redacted request received.");
        }

        [HttpPost]
        public async Task<IActionResult> GdprShopRedacted()
        {
            var data = await Request.DeserializeBodyAsync<ShopifySharp.ShopRedactedWebhook>();
            
            // Log the redaction
            var message = $"Shop {data.ShopId} ({data.ShopDomain}) has been redacted.";
            
            _logger.LogWarning(message);

            // Delete the user's account if it exists
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.ShopifyShopId == data.ShopId);

            if (user != null)
            {
                // Also delete any oauth states belonging to the user
                var oauthStates = await _dataContext.States
                    .Where(s => s.ShopifyShopDomain == user.ShopifyShopDomain)
                    .ToListAsync();
                _dataContext.States.RemoveRange(oauthStates);
                _dataContext.Users.Remove(user);

                await _dataContext.SaveChangesAsync();
            }

            return Ok("GDPR shop redacted request received.");
        }
    }
}
