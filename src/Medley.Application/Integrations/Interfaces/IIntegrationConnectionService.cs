using Medley.Domain.Entities;
using Medley.Domain.Enums;
using Medley.Application.Interfaces;

namespace Medley.Application.Integrations.Interfaces;

/// <summary>
/// Base interface for integration-specific services
/// </summary>
public interface IIntegrationConnectionService
{
    IntegrationType IntegrationType { get; }
    bool ValidateConfiguration(Dictionary<string, string> config);
    Task<bool> TestConnectionAsync(Integration integration);
    Task<ConnectionStatus> GetConnectionStatusAsync(Integration integration);
}
