using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
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

        #endregion

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            ApplicationDbContextConfigurations.Configure(builder);
            // ApplicationDbContextConfigurations.SeedData(builder);

            builder.Entity<Resume>()
                .Property(e => e.EntryDate)
                .HasColumnType("timestamp");

            builder.Entity<Resume>()
                .Property(e => e.UpdateDate)
                .HasColumnType("timestamp");

            builder.Entity<ResumeDetail>()
            .Property(e => e.EntryDate)
            .HasColumnType("timestamp");

            builder.Entity<ResumeDetail>()
                .Property(e => e.UpdateDate)
                .HasColumnType("timestamp");

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
        }

    }
}
