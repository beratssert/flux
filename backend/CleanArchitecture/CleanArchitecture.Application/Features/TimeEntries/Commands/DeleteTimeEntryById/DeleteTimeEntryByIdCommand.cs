using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CleanArchitecture.Core.Features.TimeEntries.Commands.DeleteTimeEntryById
{
    public class DeleteTimeEntryByIdCommand : IRequest<int>
    {
        public int Id { get; set; }
    }

    public class DeleteTimeEntryByIdCommandHandler : IRequestHandler<DeleteTimeEntryByIdCommand, int>
    {
        private readonly ITimeEntryRepositoryAsync _timeEntryRepository;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public DeleteTimeEntryByIdCommandHandler(ITimeEntryRepositoryAsync timeEntryRepository, IAuthenticatedUserService authenticatedUserService)
        {
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task<int> Handle(DeleteTimeEntryByIdCommand request, CancellationToken cancellationToken)
        {
            var entry = await _timeEntryRepository.GetByIdAndUserIdAsync(request.Id, _authenticatedUserService.UserId);
            if (entry == null)
            {
                throw new ApiException("Time entry not found.");
            }

            if (entry.IsLocked)
            {
                throw new ApiException("Locked entries cannot be deleted.");
            }

            entry.DeletedAtUtc = DateTime.UtcNow;
            await _timeEntryRepository.UpdateAsync(entry);
            return entry.Id;
        }
    }
}
