using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using AuntieDot.Models;

namespace AuntieDot.Data
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options) : base(options)
        {
        }
        
        public DbSet<UserAccount> Users { get; set; }
        
        public DbSet<OauthState> States { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserAccount>().ToTable("AuntieDot_Users");
            modelBuilder.Entity<OauthState>().ToTable("AuntieDot_States");
        }
    }
}