using CleanArchitecture.Core.Exceptions;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Core.Interfaces.Repositories;
using MediatR;
using System;
using System.Text.Json;
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
        private readonly IAuditService _auditService;

        public DeleteTimeEntryByIdCommandHandler(
            ITimeEntryRepositoryAsync timeEntryRepository,
            IAuthenticatedUserService authenticatedUserService,
            IAuditService auditService = null)
        {
            _timeEntryRepository = timeEntryRepository;
            _authenticatedUserService = authenticatedUserService;
            _auditService = auditService;
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

            if (_auditService != null)
            {
                await _auditService.WriteAsync(
                    "TimeEntry",
                    entry.Id.ToString(),
                    "Delete",
                    "Time entry soft-deleted.",
                    JsonSerializer.Serialize(new
                    {
                        entry.UserId,
                        entry.ProjectId,
                        entry.EntryDate,
                        entry.StartTimeUtc,
                        entry.EndTimeUtc,
                        entry.DurationMinutes,
                        entry.IsBillable,
                        entry.SourceType,
                        entry.IsLocked
                    }),
                    JsonSerializer.Serialize(new
                    {
                        entry.DeletedAtUtc
                    }));
            }

            return entry.Id;
        }
    }
}
