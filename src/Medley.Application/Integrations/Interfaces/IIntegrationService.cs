using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Application.Interfaces;

namespace Medley.Application.Integrations.Interfaces;

/// <summary>
/// Service for managing integrations
/// </summary>
public interface IIntegrationService
{
    Task<Integration?> GetByIdAsync(Guid id);
    IQueryable<Integration> Query();
    Task SaveAsync(Integration integration);
    Task DeleteAsync(Integration integration);
    Task<bool> TestConnectionAsync(Integration integration);
    Task<ConnectionStatus> GetConnectionStatusAsync(Integration integration);
    Task UpdateHealthStatusAsync(Integration integration);
}
