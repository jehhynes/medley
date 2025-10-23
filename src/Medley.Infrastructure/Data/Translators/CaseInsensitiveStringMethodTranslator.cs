using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Npgsql.EntityFrameworkCore.PostgreSQL.Query;
using System.Reflection;

namespace Medley.Infrastructure.Data.Translators;

/// <summary>
/// Custom method call translator that translates string Contains, StartsWith, and EndsWith operations
/// with case-insensitive StringComparison values (OrdinalIgnoreCase, CurrentCultureIgnoreCase, 
/// InvariantCultureIgnoreCase) to PostgreSQL ILIKE operations.
///
/// Based on the approach from Npgsql issue #2186 and NpgsqlMethodCallTranslatorProvider.
/// </summary>
public sealed class CaseInsensitiveStringMethodTranslator : IMethodCallTranslator
{
    private static readonly MethodInfo StringContainsMethodInfo
        = typeof(string).GetMethod(
            nameof(string.Contains),
            [typeof(string), typeof(StringComparison)])!;

    private static readonly MethodInfo StringStartsWithMethodInfo
        = typeof(string).GetMethod(
            nameof(string.StartsWith),
            [typeof(string), typeof(StringComparison)])!;

    private static readonly MethodInfo StringEndsWithMethodInfo
        = typeof(string).GetMethod(
            nameof(string.EndsWith),
            [typeof(string), typeof(StringComparison)])!;

    private readonly NpgsqlSqlExpressionFactory _sqlExpressionFactory;

    public CaseInsensitiveStringMethodTranslator(NpgsqlSqlExpressionFactory sqlExpressionFactory)
    {
        _sqlExpressionFactory = sqlExpressionFactory;
    }

    public SqlExpression? Translate(SqlExpression? instance, MethodInfo method, IReadOnlyList<SqlExpression> arguments, IDiagnosticsLogger<DbLoggerCategory.Query> logger)
    {
        // Only handle string methods with StringComparison parameter
        if (method.DeclaringType != typeof(string) || arguments.Count != 2)
            return null;

        // Check if this is a string comparison method with StringComparison parameter
        if (!IsStringComparisonMethod(method))
            return null;

        // Check if the StringComparison argument is case-insensitive
        if (!IsCaseInsensitiveComparison(arguments[1]))
            return null;

        // Get the target expression (the string property/column)
        var target = instance ?? arguments[0];
        var searchValue = arguments[0];

        // Translate based on the method type
        if (method == StringContainsMethodInfo)
            return TranslateContains(target, searchValue);
        
        if (method == StringStartsWithMethodInfo)
            return TranslateStartsWith(target, searchValue);
        
        if (method == StringEndsWithMethodInfo)
            return TranslateEndsWith(target, searchValue);
        
        return null;
    }

    private static bool IsStringComparisonMethod(MethodInfo method)
    {
        // Check if this is one of our target methods by comparing with static MethodInfo properties
        return method == StringContainsMethodInfo ||
               method == StringStartsWithMethodInfo ||
               method == StringEndsWithMethodInfo;
    }

    private static bool IsCaseInsensitiveComparison(SqlExpression comparisonArg)
    {
        // Handle constant expressions
        if (comparisonArg is SqlConstantExpression constantExpression)
        {
            return constantExpression.Value is StringComparison comparison &&
                   (comparison == StringComparison.OrdinalIgnoreCase ||
                    comparison == StringComparison.CurrentCultureIgnoreCase ||
                    comparison == StringComparison.InvariantCultureIgnoreCase);
        }

        // Handle parameter expressions (when StringComparison is passed as a parameter)
        if (comparisonArg is SqlParameterExpression)
        {
            // For parameter expressions, we cannot determine the value at compile time
            // We should NOT translate these to ILIKE unless we can be certain it's case-insensitive
            // This is a limitation - parameterized StringComparison values cannot be translated
            return false;
        }

        return false;
    }

    private SqlExpression TranslateContains(SqlExpression target, SqlExpression searchValue)
    {
        // Contains: ILIKE '%value%'
        // Create pattern: '%' + escapedSearchValue + '%'
        var percentLeft = _sqlExpressionFactory.Constant("%");
        var percentRight = _sqlExpressionFactory.Constant("%");
        
        var escapedSearchValue = EscapeSpecialCharacters(searchValue);
        var pattern = _sqlExpressionFactory.Add(
            _sqlExpressionFactory.Add(percentLeft, escapedSearchValue),
            percentRight);

        return _sqlExpressionFactory.ILike(target, pattern);
    }

    private SqlExpression TranslateStartsWith(SqlExpression target, SqlExpression searchValue)
    {
        // StartsWith: ILIKE 'value%'
        // Create pattern: escapedSearchValue + '%'
        var percentRight = _sqlExpressionFactory.Constant("%");
        var escapedSearchValue = EscapeSpecialCharacters(searchValue);
        var pattern = _sqlExpressionFactory.Add(escapedSearchValue, percentRight);

        return _sqlExpressionFactory.ILike(target, pattern);
    }

    private SqlExpression TranslateEndsWith(SqlExpression target, SqlExpression searchValue)
    {
        // EndsWith: ILIKE '%value'
        // Create pattern: '%' + escapedSearchValue
        var percentLeft = _sqlExpressionFactory.Constant("%");
        var escapedSearchValue = EscapeSpecialCharacters(searchValue);
        var pattern = _sqlExpressionFactory.Add(percentLeft, escapedSearchValue);

        return _sqlExpressionFactory.ILike(target, pattern);
    }

    /// <summary>
    /// Escapes special ILIKE characters (% and _) in the search value by prefixing them with backslash.
    /// In PostgreSQL ILIKE, % and _ are wildcards, so they need to be escaped as \% and \_
    /// to be treated as literal characters.
    /// </summary>
    private SqlExpression EscapeSpecialCharacters(SqlExpression searchValue)
    {
        // Handle constant expressions
        if (searchValue is SqlConstantExpression constantExpression && constantExpression.Value is string str)
        {
            // Escape % and _ by prefixing with backslash
            var escapedString = str.Replace("%", "\\%").Replace("_", "\\_");
            return _sqlExpressionFactory.Constant(escapedString);
        }

        // For non-constant expressions, we cannot safely escape at compile time
        // This is a limitation - we should not translate these to ILIKE unless we can be certain
        // they don't contain special characters, or we need to use a different approach
        return searchValue;
    }
}
