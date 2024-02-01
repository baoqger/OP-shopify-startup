using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using AuntieDot.Models;
using AuntieDot.Extensions;

namespace AuntieDot.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }
        
        public DbSet<UserAccount> Users { get; set; }
        
        public DbSet<OauthState> States { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAccount>().ToTable("AuntieDot_Users");
            modelBuilder.Entity<OauthState>().ToTable("AuntieDot_States");
        }

        /// <summary>
        /// Gets the user's account record from the database based off of their session.
        /// </summary>
        public async Task<UserAccount> GetUserFromSessionAsync(ClaimsPrincipal userIdentity)
        {
            return await Users.FirstAsync(u => u.ShopifyShopDomain == userIdentity.GetUserShopDomain());
        }
    }
}
