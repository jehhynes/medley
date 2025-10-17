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
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> GetAsync(int id);

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
    Task SaveAsync(T entity);
}