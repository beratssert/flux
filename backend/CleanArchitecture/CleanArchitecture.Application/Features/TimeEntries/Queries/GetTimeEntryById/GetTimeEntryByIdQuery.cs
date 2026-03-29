using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.TimeEntries.Queries.GetTimeEntryById
{
    public class GetTimeEntryByIdQuery : IRequest<TimeEntry>
    {
        public int Id { get; set; }

        public class GetTimeEntryByIdQueryHandler : IRequestHandler<GetTimeEntryByIdQuery, TimeEntry>
        {
            private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
            private readonly IAuthenticatedUserService _authenticatedUserService;

            public GetTimeEntryByIdQueryHandler(ITimeEntryRepositoryAsync timeEntryRepository, IAuthenticatedUserService authenticatedUserService)
            {
                _timeEntryRepository = timeEntryRepository;
                _authenticatedUserService = authenticatedUserService;
            }

            public async Task<TimeEntry> Handle(GetTimeEntryByIdQuery request, CancellationToken cancellationToken)
            {
                var entry = await _timeEntryRepository.GetByIdAndUserIdAsync(request.Id, _authenticatedUserService.UserId);
                if (entry == null)
                {
                    throw new ApiException("Time entry not found.");
                }

                return entry;
            }
        }
    }
}
