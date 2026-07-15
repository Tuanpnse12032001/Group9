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

        public DbSet<CloudFile> CloudFiles { get; set; }

        public DbSet<User> Users { get; set; }

        public DbSet<Role> Roles { get; set; }

        public DbSet<Subject> Subjects { get; set; }

        public DbSet<Document> Documents { get; set; }

        public DbSet<ChatSession> ChatSessions { get; set; }

        public DbSet<ChatMessage> ChatMessages { get; set; }

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

            modelBuilder.Entity<Subject>(entity =>
            {
                entity.HasKey(s => s.Id);

                entity.Property(s => s.SubjectCode)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(s => s.SubjectName)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasIndex(s => s.SubjectCode)
                    .IsUnique();

                entity.HasData(
                    new Subject
                    {
                        Id = 1,
                        SubjectCode = "PRN231",
                        SubjectName = "Platform Application Development"
                    },
                    new Subject
                    {
                        Id = 2,
                        SubjectCode = "PRN212",
                        SubjectName = "Basic C# Programming"
                    },
                    new Subject
                    {
                        Id = 3,
                        SubjectCode = "PRN232",
                        SubjectName = "C# Web Application Development"
                    }
                );
            });

            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(d => d.Id);

                entity.Property(d => d.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(d => d.Description)
                    .HasMaxLength(1000);

                entity.Property(d => d.FileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(d => d.StoragePath)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(d => d.PublicId)
                    .IsRequired()
                    .HasMaxLength(500)
                    .HasDefaultValue(string.Empty);

                entity.Property(d => d.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasOne(d => d.Subject)
                    .WithMany(s => s.Documents)
                    .HasForeignKey(d => d.SubjectId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.UploadedByUser)
                    .WithMany()
                    .HasForeignKey(d => d.UploadedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ChatSession>(entity =>
            {
                entity.HasKey(cs => cs.Id);

                entity.Property(cs => cs.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.HasOne(cs => cs.User)
                    .WithMany()
                    .HasForeignKey(cs => cs.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(cs => cs.Document)
                    .WithMany()
                    .HasForeignKey(cs => cs.DocumentId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.HasKey(cm => cm.Id);

                entity.Property(cm => cm.Sender)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(cm => cm.Content)
                    .IsRequired();

                entity.HasOne(cm => cm.ChatSession)
                    .WithMany(cs => cs.Messages)
                    .HasForeignKey(cm => cm.ChatSessionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<CloudFile>(entity =>
            {
                entity.HasKey(f => f.Id);

                entity.Property(f => f.OriginalFileName)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(f => f.PublicId)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(f => f.SecureUrl)
                    .IsRequired()
                    .HasMaxLength(1000);

                entity.Property(f => f.ResourceType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(f => f.ContentType)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(f => f.Status)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasOne(f => f.User)
                    .WithMany()
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}