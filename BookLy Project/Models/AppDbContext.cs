using Microsoft.EntityFrameworkCore;

namespace BookLy_Project.Models
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<BorrowRequest> BorrowRequests { get; set; }
        public DbSet<BorrowDetail> BorrowDetails { get; set; }
        public DbSet<Favorite> Favorites { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BorrowRequest>()
                .HasOne(b => b.User)
                .WithMany(u => u.BorrowRequests)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<BorrowRequest>()
                .HasOne(b => b.Approver)
                .WithMany(u => u.ApprovedRequests)
                .HasForeignKey(b => b.ApprovedBy)
                .OnDelete(DeleteBehavior.NoAction);
        }
    }
}
