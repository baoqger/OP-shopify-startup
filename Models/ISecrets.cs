namespace AuntieDot.Models
{
    public interface ISecrets
    {
        string ShopifySecretKey { get; }
        string ShopifyApiKey { get; }
        string HostDomain { get; }
    }
}
