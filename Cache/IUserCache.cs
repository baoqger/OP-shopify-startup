using AuntieDot.Models;

namespace AuntieDot.Cache
{
    public interface IUserCache
    {
        /// <summary>
        /// Deletes the shop's status from cache.
        /// </summary>
        void ResetShopStatus(int userId);

        /// <summary>
        /// Returns details about the given user's shop status.
        /// </summary>
        CachedShopStatus GetShopStatus(int userId);
    }
}
