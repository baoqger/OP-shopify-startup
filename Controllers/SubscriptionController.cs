using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using AuntieDot.Attributes;
using AuntieDot.Data;
using AuntieDot.Extensions;
using AuntieDot.Models;
using ShopifySharp;

namespace AuntieDot.Controllers
{
    [Authorize]
    public class SubscriptionController : Controller
    {
        public SubscriptionController(DataContext userContext, IHostEnvironment environment, IApplicationUrls urls)
        {
            _dataContext = userContext;
            _environment = environment;
            _urls = urls;
        }
        
        private readonly DataContext _dataContext;
        private readonly IHostEnvironment _environment;
        private readonly IApplicationUrls _urls;

        [HttpGet]
        public async Task<IActionResult> Start()
        {
            // Make sure the user isn't already subscribed
            var user = await _dataContext.GetUserFromSessionAsync(User);

            if (user.HasActiveSubscription)
            {
                return RedirectToAction("Index", "Home");
            }
            
            return View(new SubscribeViewModel());
        }
        
        [HttpPost]
        public async Task<IActionResult> HandleStartSubscription()
        {
            // Grab the user's account and check if they already have a charge
            var user = await _dataContext.GetUserFromSessionAsync(User);
            var service = new RecurringChargeService(user.ShopifyShopDomain, user.ShopifyAccessToken);

            if (user.HasActiveSubscription)
            {
                // The user is already subscribed. Redirect them to the home page.
                return RedirectToAction("Index", "Home");
            }

            if (user.HasActiveSubscription)
            {
                // The user already has a charge. Make sure it hasn't already been accepted.
                var existingCharge = await service.GetAsync(user.ShopifyChargeId.Value);

                if (existingCharge.Status == "active")
                {
                    // The charge was activated by the user, but somehow they've ended up back here. Update the user's subscription
                    // details then send them to the home page.
                    user.BillingOn = existingCharge.BillingOn;

                    await _dataContext.SaveChangesAsync();
                    await HttpContext.SignInAsync(user);

                    return RedirectToAction("Index", "Home");
                }

                if (existingCharge.Status == "pending")
                {
                    // The previous charge hasn't been accepted but also hasn't expired. Send them back to the charge URL.
                    return Redirect(existingCharge.ConfirmationUrl);
                }
            }
            
            var charge = await service.CreateAsync(new RecurringCharge
            { 
                TrialDays = 7,
                Name = "My App Subscription Plan",
                Price = 9.99M,
                ReturnUrl = _urls.SubscriptionRedirectUrl,
                Test = _environment.IsDevelopment()
            });

            // Save the charge ID in the database so we can pull in the user's account in the next step without them being logged in
            user.ShopifyChargeId = charge.Id;
            user.BillingOn = null;

            await _dataContext.SaveChangesAsync();
            await HttpContext.SignInAsync(user);

            return Redirect(charge.ConfirmationUrl);
        }

        [HttpGet]
        public async Task<IActionResult> ChargeResult([FromQuery(Name = "charge_id")] long chargeId)
        {
            // This user is likely to be anonymous because Shopify does not redirect them back to the embedded app after
            // accepting a charge. We need to pull in the user's account based on the charge ID
            var user = await _dataContext.Users.FirstOrDefaultAsync(u => u.ShopifyChargeId == chargeId);
            
            // Get the subscription they're activating
            var service = new RecurringChargeService(user.ShopifyShopDomain, user.ShopifyAccessToken);
            var charge = await service.GetAsync(chargeId);

            // Check the status of the charge
            switch (charge.Status)
            {
                case "pending":
                    // User has not accepted or declined the charge. Send them back to the confirmation url
                    return Redirect(charge.ConfirmationUrl);
                
                case "expired":
                case "declined":
                    // The charge expired or declined. Prompt the user to accept a new charge.
                    return RedirectToAction("Start");
                
                case "active":
                    // User has activated the charge, update their account and session.
                    user.ShopifyChargeId = chargeId;
                    user.BillingOn = charge.BillingOn;

                    await _dataContext.SaveChangesAsync();
                    await HttpContext.SignInAsync(user);

                    // User's subscription has been activated, they can now use the app
                    return RedirectToAction("Index", "Home");
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(charge.Status), $"Unhandled Shopify charge status of {charge.Status}");
            }
        }

        [AuthorizeWithActiveSubscription]
        public async Task<IActionResult> Index()
        {
            var user = await _dataContext.GetUserFromSessionAsync(User);

            if (!user.HasActiveSubscription)
            {
                return RedirectToAction("Start");
            }
            
            // Pull in the user's subscription data from Shopify
            var chargeService = new RecurringChargeService(user.ShopifyShopDomain, user.ShopifyAccessToken);
            RecurringCharge charge;

            try
            {
                charge = await chargeService.GetAsync(user.ShopifyChargeId.Value);
            }
            catch (ShopifyException e) when (e.HttpStatusCode == HttpStatusCode.NotFound)
            {
                // The user's subscription no longer exists. Update their user model to delete their charge ID
                user.ShopifyChargeId = null;
                user.BillingOn = null;

                await _dataContext.SaveChangesAsync();
                
                // Update the user's session, then redirect them to the subscription page to accept a new charge
                await HttpContext.SignInAsync(user);

                return RedirectToAction("Start");
            }
            
            return View(new SubscriptionViewModel(charge));
        }
    }
}
