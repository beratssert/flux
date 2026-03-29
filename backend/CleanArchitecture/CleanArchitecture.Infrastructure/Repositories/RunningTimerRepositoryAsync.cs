using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces.Repositories;
using CleanArchitecture.Infrastructure.Contexts;
using CleanArchitecture.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Repositories
{
    public class RunningTimerRepositoryAsync : GenericRepositoryAsync<RunningTimer>, IRunningTimerRepositoryAsync
    {
        private readonly DbSet<RunningTimer> _runningTimers;

        public RunningTimerRepositoryAsync(ApplicationDbContext dbContext) : base(dbContext)
        {
            _runningTimers = dbContext.Set<RunningTimer>();
        }

        public Task<RunningTimer> GetActiveByUserIdAsync(string userId)
        {
            return _runningTimers.FirstOrDefaultAsync(rt => rt.UserId == userId);
        }
    }
}
