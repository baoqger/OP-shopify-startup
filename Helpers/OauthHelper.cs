using System;
using System.Threading.Tasks;
using ShopifySharp;
using AuntieDot.Data;
using AuntieDot.Models;

namespace AuntieDot.Helpers
{
    public class OauthHelper : IOauthHelper
    {
        private readonly DataContext _dataContext;
        private readonly ISecrets _secrets;
        private readonly IApplicationUrls _appUrls;

        public OauthHelper(DataContext dataContext, ISecrets secrets, IApplicationUrls appUrls)
        {
            _dataContext = dataContext;
            _secrets = secrets;
            _appUrls = appUrls;
        }

        public async Task<Uri> CreateOauthUrl(string shop)
        {
            var requiredPermissions = new [] { "read_orders" };
            // Create a new oauthstate token and save it to the database 
            var oauthState = await _dataContext.States.AddAsync(new OauthState
            {
                DateCreated = DateTimeOffset.Now,
                Token = Guid.NewGuid().ToString()
            });

            await _dataContext.SaveChangesAsync();
            
            return AuthorizationService.BuildAuthorizationUrl(
                requiredPermissions, 
                shop,
                _secrets.ShopifyApiKey, 
                _appUrls.OauthRedirectUrl, 
                oauthState.Entity.Token
            );
        }
    }
}
