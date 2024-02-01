using System;

namespace AuntieDot.Models
{
    public class UserAccount
    {
        public int Id { get; set; }
        
        ///<summary>
        /// The user's ShopifyAccessToken, received from Shopify's OAuth integration flow.
        ///</summary>
        public string ShopifyAccessToken { get; set; }
        
        ///<summary>
        /// The user's *.myshopify.com domain.
        ///</summary>
        public string ShopifyShopDomain { get; set; }
        
        /// <summary>
        /// The id of the user's shop. 
        /// </summary>
        public long ShopifyShopId { get; set; }
        
        ///<summary>
        ///The id of the user's Shopify subscription charge.
        ///</summary>
        public long? ShopifyChargeId { get; set; }
        
        /// <summary>
        /// The date that the customer will next be billed on.
        /// </summary>
        public DateTimeOffset? BillingOn { get; set; }

        /// <summary>
        /// Indicates the user has an active Shopify subscription charge. True when the <see cref="ShopifyChargeId" /> and <see cref="BillingOn" /> fields both have values.
        /// </summary>
        public bool HasActiveSubscription => ShopifyChargeId.HasValue && BillingOn.HasValue;
    }
}
