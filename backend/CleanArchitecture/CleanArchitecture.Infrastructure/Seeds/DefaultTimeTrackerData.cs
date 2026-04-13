using CleanArchitecture.Core.Entities;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Seeds
{
    public static class DefaultTimeTrackerData
    {
        public static async Task SeedAsync(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            var basicUser = await userManager.FindByEmailAsync("employee@flux.local");
            var managerUser = await userManager.FindByEmailAsync("manager@flux.local");

            if (basicUser == null || managerUser == null)
            {
                return;
            }

            if (!await dbContext.Projects.AnyAsync())
            {
                dbContext.Projects.Add(new Project
                {
                    Name = "Flux Internal",
                    ManagerUserId = managerUser.Id,
                    Status = "Active"
                });

                dbContext.Projects.Add(new Project
                {
                    Name = "Clockify Clone MVP",
                    ManagerUserId = managerUser.Id,
                    Status = "Active"
                });

                await dbContext.SaveChangesAsync();
            }

            var defaultProject = await dbContext.Projects.OrderBy(p => p.Id).FirstAsync();

            var hasAssignment = await dbContext.ProjectAssignments.AnyAsync(pa =>
                pa.UserId == basicUser.Id &&
                pa.ProjectId == defaultProject.Id &&
                pa.IsActive);

            if (!hasAssignment)
            {
                dbContext.ProjectAssignments.Add(new ProjectAssignment
                {
                    UserId = basicUser.Id,
                    ProjectId = defaultProject.Id,
                    AssignedAtUtc = DateTime.UtcNow,
                    AssignedByUserId = managerUser.Id,
                    IsActive = true
                });

                await dbContext.SaveChangesAsync();
            }

            var hasTimeEntries = await dbContext.TimeEntries.AnyAsync(te => te.UserId == basicUser.Id && te.DeletedAtUtc == null);
            if (hasTimeEntries)
            {
                return;
            }

            var today = DateTime.UtcNow.Date;

            dbContext.TimeEntries.Add(new TimeEntry
            {
                UserId = basicUser.Id,
                ProjectId = defaultProject.Id,
                EntryDate = today,
                StartTimeUtc = today.AddHours(9),
                EndTimeUtc = today.AddHours(10),
                DurationMinutes = 60,
                Description = "Seeded focus session",
                IsBillable = false,
                SourceType = "Manual",
                IsLocked = false
            });

            dbContext.TimeEntries.Add(new TimeEntry
            {
                UserId = basicUser.Id,
                ProjectId = defaultProject.Id,
                EntryDate = today,
                StartTimeUtc = today.AddHours(10.5),
                EndTimeUtc = today.AddHours(12),
                DurationMinutes = 90,
                Description = "Seeded pair session",
                IsBillable = true,
                SourceType = "Manual",
                IsLocked = false
            });

            await dbContext.SaveChangesAsync();
        }
    }
}
