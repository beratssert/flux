using CleanArchitecture.Core.Entities;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces.Repositories
{
    public interface IRunningTimerRepositoryAsync : IGenericRepositoryAsync<RunningTimer>
    {
        Task<RunningTimer> GetActiveByUserIdAsync(string userId);
    }
}
