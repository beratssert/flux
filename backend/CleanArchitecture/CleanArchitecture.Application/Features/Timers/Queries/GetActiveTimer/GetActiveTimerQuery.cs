using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.Timers.Queries.GetActiveTimer
{
    public class GetActiveTimerQuery : IRequest<RunningTimer>
    {
    }

    public class GetActiveTimerQueryHandler : IRequestHandler<GetActiveTimerQuery, RunningTimer>
    {
        private readonly IRunningTimerRepositoryAsync _runningTimerRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public GetActiveTimerQueryHandler(IRunningTimerRepositoryAsync runningTimerRepository, IAuthenticatedUserService authenticatedUserService)
        {
            _runningTimerRepository = runningTimerRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public Task<RunningTimer> Handle(GetActiveTimerQuery request, CancellationToken cancellationToken)
        {
            return _runningTimerRepository.GetActiveByUserIdAsync(_authenticatedUserService.UserId);
        }
    }
}
