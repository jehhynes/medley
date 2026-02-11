namespace Medley.Application.Interfaces;

/// <summary>
/// Generic repository interface for data access operations
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets an entity by its identifier
    /// </summary>
    /// <param name="id">The entity identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a queryable collection of entities
    /// </summary>
    /// <returns>IQueryable for the entity type</returns>
    IQueryable<T> Query();

    /// <summary>
    /// Saves an entity (insert or update)
    /// </summary>
    /// <param name="entity">The entity to save</param>
    /// <returns>A task representing the asynchronous operation</returns>
    Task Add(T entity);

    /// <summary>
    /// Deletes an entity
    /// </summary>
    /// <param name="entity">The entity to delete</param>
    Task Remove(T entity);
}