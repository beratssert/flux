using CleanArchitecture.Core.Enums;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Seeds
{
    public static class DefaultModerator
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            //Seed Default Moderator User
            var defaultUser = new ApplicationUser
            {
                UserName = "moderatoruser",
                Email = "moderatoruser@gmail.com",
                FirstName = "Moderator",
                LastName = "User",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            if (userManager.Users.All(u => u.Id != defaultUser.Id))
            {
                var user = await userManager.FindByEmailAsync(defaultUser.Email);
                if (user == null)
                {
                    await userManager.CreateAsync(defaultUser, "123Pa$$word!");
                    await userManager.AddToRoleAsync(defaultUser, Roles.Basic.ToString());
                    await userManager.AddToRoleAsync(defaultUser, Roles.Moderator.ToString());
                }
            }
        }
    }
}