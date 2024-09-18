using Microsoft.EntityFrameworkCore;
using ShoppingList.Entities;

namespace ShoppingList
{
    public class ApplicationContext(DbContextOptions<ApplicationContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Item> Items { get; set; } = null!;
        public DbSet<UserItem> UserItems { get; set; } = null!;
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>().HasMany(u => u.UserItems).WithOne(i => i.User).HasForeignKey(i => i.UserId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<User>().HasAlternateKey(u => u.Login);
            modelBuilder.Entity<User>().Property(i => i.Balance).HasPrecision(18, 2); // Точность 18, масштаб 2

            modelBuilder.Entity<Item>().HasMany(i => i.UserItems).WithOne(i => i.Item).HasForeignKey(i => i.ItemId).OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Item>().Property(i => i.Price).HasPrecision(18, 2); // Точность 18, масштаб 2

            modelBuilder.Entity<UserItem>().HasKey(ui => new { ui.UserId, ui.ItemId });

            base.OnModelCreating(modelBuilder);
        }
    }
}
