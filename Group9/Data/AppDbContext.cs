using Group9.Models;
using Microsoft.EntityFrameworkCore;

namespace Group9.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Role> Roles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>(entity =>
            {
                entity.HasKey(r => r.Id);

                entity.Property(r => r.RoleName)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(r => r.RoleName)
                    .IsUnique();

                entity.HasData(
                    new Role { Id = 1, RoleName = "Guest" },
                    new Role { Id = 2, RoleName = "User" },
                    new Role { Id = 3, RoleName = "Admin" },
                    new Role { Id = 4, RoleName = "ChatbotService" }
                );
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(u => u.Id);

                entity.Property(u => u.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(u => u.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.HasIndex(u => u.Email)
                    .IsUnique();

                entity.Property(u => u.PasswordHash)
                    .IsRequired();

                entity.Property(u => u.Phone)
                    .HasMaxLength(20);

                entity.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.RoleId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}