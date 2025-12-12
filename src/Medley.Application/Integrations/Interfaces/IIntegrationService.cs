using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Application.Interfaces;

namespace Medley.Application.Integrations.Interfaces;

/// <summary>
/// Service for managing integrations
/// </summary>
public interface IIntegrationService
{
    Task<Integration?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    IQueryable<Integration> Query();
    Task SaveAsync(Integration integration, CancellationToken cancellationToken = default);
    Task DeleteAsync(Integration integration, CancellationToken cancellationToken = default);
    Task<ConnectionStatus> TestConnectionAsync(Integration integration, CancellationToken cancellationToken = default);
}
