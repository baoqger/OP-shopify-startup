namespace AuntieDot.Models
{
    public class Session
    {
        public Session(UserAccount userAccount = null)
        {
            if (userAccount != null)
            {
                UserId = userAccount.Id;
                ShopifyShopDomain = userAccount.ShopifyShopDomain;
                IsSubscribed = userAccount.ShopifyChargeId.HasValue;
            }
        }
        
        public int UserId { get; set; }
        public string ShopifyShopDomain { get; set; }
        public bool IsSubscribed { get; set; }
    }
}
