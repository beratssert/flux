using CleanArchitecture.Core.Enums;
using CleanArchitecture.Infrastructure.Models;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Seeds
{
    public static class DefaultManagerUser
    {
        public static async Task SeedAsync(UserManager<ApplicationUser> userManager)
        {
            var defaultUser = new ApplicationUser
            {
                UserName = "manager",
                Email = "manager@flux.local",
                FirstName = "Mina",
                LastName = "Manager",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                Status = UserStatus.Active.ToString()
            };

            var user = await userManager.FindByEmailAsync(defaultUser.Email);
            if (user != null)
            {
                return;
            }

            await userManager.CreateAsync(defaultUser, "123Pa$$word!");
            await userManager.AddToRoleAsync(defaultUser, Roles.Manager.ToString());
        }
    }
}
