using Microsoft.EntityFrameworkCore;
using POS_91Cafe.Models;

namespace POS_91Cafe.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<Product> Products { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<SaleDetail> SaleDetails { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<ProductIngredient> ProductIngredients { get; set; }
        public DbSet<Supplier> Suppliers { get; set; }
        public DbSet<AppSetting> AppSettings { get; set; }

        // Added Users DbSet for Authentication
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // You can add composite key configurations here if needed,
            // but for now, the default conventions should work.
        }
    }
}