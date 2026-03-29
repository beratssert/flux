using CleanArchitecture.Core.Enums;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Seeds
{
    public static class DefaultRoles
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            await EnsureRoleExistsAsync(roleManager, Roles.Employee.ToString());
            await EnsureRoleExistsAsync(roleManager, Roles.Manager.ToString());
            await EnsureRoleExistsAsync(roleManager, Roles.Admin.ToString());
        }

        private static async Task EnsureRoleExistsAsync(RoleManager<IdentityRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }
    }
}
