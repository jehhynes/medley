using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query.ExpressionTranslators.Internal;
using System.Diagnostics.CodeAnalysis;

namespace Medley.Infrastructure.Data.Translators;

/// <summary>
/// Custom method call translator provider that extends Npgsql's provider
/// to include case-insensitive string method translations.
///
/// Based on the approach from Npgsql issue #2186.
/// </summary>
[SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
public sealed class CustomNpgsqlMethodCallTranslatorProvider : NpgsqlMethodCallTranslatorProvider
{
    public CustomNpgsqlMethodCallTranslatorProvider(RelationalMethodCallTranslatorProviderDependencies dependencies,
        IModel model,
        IDbContextOptions contextOptions) 
        : base(dependencies, model, contextOptions)
    {
        var sqlExpressionFactory = (NpgsqlSqlExpressionFactory)dependencies.SqlExpressionFactory;

        // Add our custom translator
        AddTranslators(
        [
            new CaseInsensitiveStringMethodTranslator(sqlExpressionFactory)
        ]);
    }
}
