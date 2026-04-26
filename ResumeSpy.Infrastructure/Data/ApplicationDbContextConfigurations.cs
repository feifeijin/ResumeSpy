using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResumeSpy.Core.Entities.General;

namespace ResumeSpy.Infrastructure.Data
{
    public class ApplicationDbContextConfigurations
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");

            modelBuilder.Entity<PromptTemplate>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.Key).HasMaxLength(100).IsRequired();
                entity.Property(e => e.Category).HasMaxLength(50).IsRequired();
                entity.Property(e => e.SystemMessage).IsRequired();
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.HasIndex(e => e.Key).IsUnique();
            });
        }

        public static void SeedData(ModelBuilder modelBuilder)
        {
            // Add any seed data here
            // modelBuilder.Entity<Resume>().HasData(
            // new Resume
            // {
            //     Id = Guid.NewGuid().ToString(),
            //     Title = "Resume 1",
            //     ResumeDetailCount = 3,
            //     ResumeImgPath = "/assets/discover_bg.png",
            //     EntryDate = DateTime.UtcNow,
            //     UpdateDate = DateTime.UtcNow
            // },
            // new Resume
            // {
            //     Id = Guid.NewGuid().ToString(),
            //     Title = "Resume 2",
            //     ResumeDetailCount = 2,
            //     ResumeImgPath = "/assets/discover_bg.png",
            //     EntryDate = DateTime.UtcNow,
            //     UpdateDate = DateTime.UtcNow

            // },
            // new Resume
            // {
            //     Id = Guid.NewGuid().ToString(),
            //     Title = "Resume 3",
            //     ResumeDetailCount = 5,
            //     ResumeImgPath = "/assets/discover_bg.png",
            //     EntryDate = DateTime.UtcNow,
            //     UpdateDate = DateTime.UtcNow
            // },
            // new Resume
            // {
            //     Id = Guid.NewGuid().ToString(),
            //     Title = "Resume 4",
            //     ResumeDetailCount = 1,
            //     ResumeImgPath = "/assets/discover_bg.png",
            //     EntryDate = DateTime.UtcNow,
            //     UpdateDate = DateTime.UtcNow
            // },
            // new Resume
            // {
            //     Id = Guid.NewGuid().ToString(),
            //     Title = "Resume 5",
            //     ResumeDetailCount = 4,
            //     ResumeImgPath = "/assets/discover_bg.png",
            //     EntryDate = DateTime.UtcNow,
            //     UpdateDate = DateTime.UtcNow
            // }
            // );

        }

    }
}
