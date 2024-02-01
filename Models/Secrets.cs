using System;
using Microsoft.Extensions.Configuration;

namespace AuntieDot.Models
{
    public class Secrets : ISecrets
    {
        public Secrets(IConfiguration config)
        {
            _config = config;
            ShopifySecretKey = Find("SHOPIFY_SECRET_KEY");
            ShopifyApiKey = Find("SHOPIFY_API_KEY");
            HostDomain = Find("HOST_DOMAIN");
        }

        private readonly IConfiguration _config;

        string Find(string key)
        {
            var value = _config.GetValue<string>(key);

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new NullReferenceException(key);
            }

            return value;
        }

        public string ShopifySecretKey { get; }
        public string ShopifyApiKey { get; }
        public string HostDomain { get; }
    }
}
