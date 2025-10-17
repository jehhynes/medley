namespace Medley.Application.Interfaces;

/// <summary>
/// Factory interface for creating database contexts
/// </summary>
/// <typeparam name="T">The database context type</typeparam>
public interface IDbContextFactory<T> where T : class
{
    /// <summary>
    /// Creates a new instance of the specified database context
    /// </summary>
    /// <returns>A new database context instance</returns>
    T CreateDbContext();
}