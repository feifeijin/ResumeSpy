using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ResumeSpy.Core.Entities.General;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ResumeSpy.Infrastructure.Data
{
    public class ApplicationDbContextConfigurations
    {
        public static void Configure(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationUser>().ToTable("Users");
            modelBuilder.Entity<IdentityRole>().ToTable("Roles");

            // Add any additional entity configurations here
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
