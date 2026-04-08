using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Enums;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Seeds
{
    public static class DefaultExpenseData
    {
        public static async Task SeedAsync(ApplicationDbContext dbContext, UserManager<ApplicationUser> userManager)
        {
            if (!await dbContext.ExpenseCategories.AnyAsync())
            {
                dbContext.ExpenseCategories.AddRange(
                    new ExpenseCategory { Name = "Transportation", IsActive = true },
                    new ExpenseCategory { Name = "Meals", IsActive = true },
                    new ExpenseCategory { Name = "Office Supplies", IsActive = true });
                await dbContext.SaveChangesAsync();
            }

            var basicUser = await userManager.FindByEmailAsync("employee@flux.local");
            if (basicUser == null)
            {
                return;
            }

            var hasExpenses = await dbContext.Expenses.AnyAsync(e => e.UserId == basicUser.Id && e.DeletedAtUtc == null);
            if (hasExpenses)
            {
                return;
            }

            var assignment = await dbContext.ProjectAssignments
                .Where(pa => pa.UserId == basicUser.Id && pa.IsActive)
                .OrderBy(pa => pa.Id)
                .FirstOrDefaultAsync();
            var category = await dbContext.ExpenseCategories.OrderBy(c => c.Id).FirstAsync();
            if (assignment == null)
            {
                return;
            }

            dbContext.Expenses.Add(new Expense
            {
                UserId = basicUser.Id,
                ProjectId = assignment.ProjectId,
                ExpenseDate = System.DateTime.UtcNow.Date,
                Amount = 150.50m,
                CurrencyCode = "TRY",
                CategoryId = category.Id,
                Notes = "Seeded transportation expense",
                Status = ExpenseStatuses.Draft
            });

            await dbContext.SaveChangesAsync();
        }
    }
}
