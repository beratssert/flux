using System.Collections.Generic;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IUserRolesService
    {
        Task<IReadOnlyList<string>> GetRolesAsync(string userId);

        Task<bool> UserExistsAsync(string userId);
    }
}
