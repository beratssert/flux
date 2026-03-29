using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces.Repositories
{
    public interface IProjectAssignmentRepositoryAsync
    {
        Task<bool> IsUserAssignedToProjectAsync(string userId, int projectId);
    }
}
