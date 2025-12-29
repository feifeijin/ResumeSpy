using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        #region DbSet Section
        public DbSet<Resume> Resumes { get; set; }
        public DbSet<ResumeDetail> ResumeDetails { get; set; }
        public DbSet<UserRefreshToken> UserRefreshTokens { get; set; }
        public DbSet<EmailLoginToken> EmailLoginTokens { get; set; }
        public DbSet<GuestSession> GuestSessions { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ApplicationDbContextConfigurations.Configure(builder);
            // ApplicationDbContextConfigurations.SeedData(builder);

            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : (DateTime?)null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : (DateTime?)null);

            foreach (var entityType in builder.Model.GetEntityTypes())
            {
                foreach (var property in entityType.GetProperties())
                {
                    if (property.ClrType == typeof(DateTime))
                    {
                        property.SetValueConverter(dateTimeConverter);
                    }
                    else if (property.ClrType == typeof(DateTime?))
                    {
                        property.SetValueConverter(nullableDateTimeConverter);
                    }
                }
            }

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.DisplayName).HasMaxLength(128);
                entity.Property(e => e.AvatarUrl).HasMaxLength(512);
                entity.Property(e => e.JobTitle).HasMaxLength(128);
                entity.Property(e => e.Organization).HasMaxLength(128);
            });

            builder.Entity<UserRefreshToken>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.HasIndex(e => e.Token).IsUnique();
                entity.HasOne(e => e.User)
                    .WithMany(u => u.RefreshTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<EmailLoginToken>(entity =>
            {
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.TokenHash)
                    .IsRequired()
                    .HasMaxLength(128);
                entity.Property(e => e.RedirectUrl)
                    .HasMaxLength(1024);
                entity.HasIndex(e => new { e.UserId, e.TokenHash }).IsUnique();
                entity.HasOne(e => e.User)
                    .WithMany(u => u.EmailLoginTokens)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Resume>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.GuestSessionId);
                entity.HasIndex(e => new { e.UserId, e.GuestSessionId });
                
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Resumes)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.GuestSession)
                    .WithMany()
                    .HasForeignKey(e => e.GuestSessionId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<GuestSession>(entity =>
            {
                entity.Property(e => e.IpAddress)
                    .IsRequired()
                    .HasMaxLength(45); // IPv6 max length
                
                entity.Property(e => e.UserAgent)
                    .HasMaxLength(512);
                
                entity.HasIndex(e => e.ExpiresAt);
                entity.HasIndex(e => e.IsConverted);
                
                entity.HasOne(e => e.ConvertedUser)
                    .WithMany()
                    .HasForeignKey(e => e.ConvertedUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

    }
}
