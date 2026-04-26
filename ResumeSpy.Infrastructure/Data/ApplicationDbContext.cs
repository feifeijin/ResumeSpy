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
        public DbSet<AnonymousUser> AnonymousUsers { get; set; }
        public DbSet<GuestSession> GuestSessions { get; set; }
        public DbSet<ResumeVersion> ResumeVersions { get; set; }
        public DbSet<UserIdentity> UserIdentities { get; set; }
        public DbSet<PromptTemplate> PromptTemplates { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ApplicationDbContextConfigurations.Configure(builder);
            // ApplicationDbContextConfigurations.SeedData(builder);

            // Configure DateTime converter for PostgreSQL timestamptz
            // Ensures all DateTime values are treated as UTC
            var dateTimeConverter = new ValueConverter<DateTime, DateTime>(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

            var nullableDateTimeConverter = new ValueConverter<DateTime?, DateTime?>(
                v => v.HasValue ? (v.Value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v.Value.ToUniversalTime()) : null,
                v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null);

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

            builder.Entity<UserIdentity>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Provider).HasMaxLength(32).IsRequired();
                entity.Property(e => e.ProviderUserId).HasMaxLength(256).IsRequired();
                entity.Property(e => e.Email).HasMaxLength(256);

                // Core uniqueness guarantee: one identity per provider+sub pair
                entity.HasIndex(e => new { e.Provider, e.ProviderUserId }).IsUnique();
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.Email);

                entity.HasOne(e => e.User)
                    .WithMany(u => u.Identities)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<Resume>(entity =>
            {
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.AnonymousUserId);
                entity.HasIndex(e => new { e.UserId, e.AnonymousUserId });
                
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Resumes)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                
                entity.HasOne(e => e.AnonymousUser)
                    .WithMany()
                    .HasForeignKey(e => e.AnonymousUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<ResumeVersion>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedNever();
                entity.HasIndex(e => e.ResumeDetailId);
                entity.HasIndex(e => e.CreatedAt);

                entity.HasOne(e => e.ResumeDetail)
                    .WithMany()
                    .HasForeignKey(e => e.ResumeDetailId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<AnonymousUser>(entity =>
            {
                entity.HasIndex(e => e.IsConverted);
                
                entity.HasOne(e => e.ConvertedUser)
                    .WithMany()
                    .HasForeignKey(e => e.ConvertedUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });
        }

    }
}
