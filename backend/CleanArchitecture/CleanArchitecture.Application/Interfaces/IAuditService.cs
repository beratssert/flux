using System.Threading.Tasks;

namespace CleanArchitecture.Core.Interfaces
{
    public interface IAuditService
    {
        Task WriteAsync(
            string entityName,
            string entityId,
            string actionType,
            string note = null,
            string oldValuesJson = null,
            string newValuesJson = null);
    }
}
