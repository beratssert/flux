using CleanArchitecture.Core.Entities;
using CleanArchitecture.Core.Interfaces;
using CleanArchitecture.Infrastructure.Contexts;
using System;
using System.Threading.Tasks;

namespace CleanArchitecture.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IAuthenticatedUserService _authenticatedUserService;

        public AuditService(ApplicationDbContext dbContext, IAuthenticatedUserService authenticatedUserService)
        {
            _dbContext = dbContext;
            _authenticatedUserService = authenticatedUserService;
        }

        public async Task WriteAsync(
            string entityName,
            string entityId,
            string actionType,
            string note = null,
            string oldValuesJson = null,
            string newValuesJson = null)
        {
            var log = new AuditLog
            {
                ActorUserId = _authenticatedUserService.UserId,
                EntityName = entityName,
                EntityId = entityId,
                ActionType = actionType,
                OldValuesJson = oldValuesJson,
                NewValuesJson = newValuesJson,
                OccurredAtUtc = DateTime.UtcNow,
                Note = note
            };

            _dbContext.AuditLogs.Add(log);
            await _dbContext.SaveChangesAsync();
        }
    }
}
