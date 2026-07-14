using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace ECommerce.Domain.Specifications;

public sealed class StringSpecification<T> : ISpecification<T> where T : class
{
    private readonly List<string> _whereClauses = [];
    private readonly List<(string Property, bool Descending)> _orderByClauses = [];
    private readonly List<string> _includePaths = [];

    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public IReadOnlyList<Expression<Func<T, object>>> Includes { get; } = [];
    public IReadOnlyList<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderExpressions { get; } = [];
    public Expression<Func<T, object>>? GroupBy => null;
    public int? SkipValue { get; private set; }
    public int? TakeValue { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool IsAsNoTracking { get; private set; }
    public bool IsAsSplitQuery { get; private set; }
    public bool IgnoreQueryFilters { get; private set; }

    int? ISpecification<T>.Skip => SkipValue;
    int? ISpecification<T>.Take => TakeValue;

    public StringSpecification<T> Where(string predicate)
    {
        _whereClauses.Add(predicate);
        Criteria = BuildCriteriaExpression();
        return this;
    }

    public StringSpecification<T> And(string predicate)
    {
        _whereClauses.Add(predicate);
        Criteria = BuildCriteriaExpression();
        return this;
    }

    public StringSpecification<T> Include(string propertyPath)
    {
        _includePaths.Add(propertyPath);
        return this;
    }

    public StringSpecification<T> OrderBy(string property)
    {
        _orderByClauses.Add((property, false));
        return this;
    }

    public StringSpecification<T> OrderByDescending(string property)
    {
        _orderByClauses.Add((property, true));
        return this;
    }

    public StringSpecification<T> ApplySkip(int count)
    {
        SkipValue = count;
        IsPagingEnabled = true;
        return this;
    }

    public StringSpecification<T> ApplyTake(int count)
    {
        TakeValue = count;
        IsPagingEnabled = true;
        return this;
    }

    public StringSpecification<T> AsNoTracking()
    {
        IsAsNoTracking = true;
        return this;
    }

    public bool IsSatisfiedBy(T entity)
    {
        if (Criteria is null) return true;

        var predicate = Criteria.Compile();
        return predicate(entity);
    }

    private Expression<Func<T, bool>>? BuildCriteriaExpression()
    {
        if (_whereClauses.Count == 0)
            return null;

        var parameter = Expression.Parameter(typeof(T));
        Expression? combined = null;

        foreach (var clause in _whereClauses)
        {
            var parsed = StringExpressionParser.ParsePredicate<T>(parameter, clause);
            combined = combined is null ? parsed : Expression.AndAlso(combined, parsed);
        }

        return Expression.Lambda<Func<T, bool>>(combined!, parameter);
    }
}

internal static class StringExpressionParser
{
    public static Expression ParsePredicate<T>(ParameterExpression parameter, string predicate) where T : class
    {
        predicate = predicate.Trim();

        if (predicate.Contains(".Contains("))
            return ParseContainsCall<T>(parameter, predicate);

        if (predicate.Contains(" > ") || predicate.Contains(" < ") ||
            predicate.Contains(" >= ") || predicate.Contains(" <= ") ||
            predicate.Contains(" == ") || predicate.Contains(" != "))
            return ParseComparison<T>(parameter, predicate);

        throw new ArgumentException($"Unsupported predicate format: '{predicate}'");
    }

    private static Expression ParseContainsCall<T>(ParameterExpression parameter, string predicate) where T : class
    {
        var dotIndex = predicate.IndexOf(".Contains(");
        var propName = predicate[..dotIndex].Trim();

        var valueStart = predicate.IndexOf('(', dotIndex) + 1;
        var valueEnd = predicate.IndexOf(')', valueStart);
        var value = predicate[valueStart..valueEnd].Trim(' ', '"', '\'');

        var property = GetPropertyExpression<T>(parameter, propName);
        var valueConstant = Expression.Constant(value);
        var containsMethod = typeof(string).GetMethod("Contains", [typeof(string)])!;

        return Expression.Call(property, containsMethod, valueConstant);
    }

    private static Expression ParseComparison<T>(ParameterExpression parameter, string predicate) where T : class
    {
        string[] ops = ["!=", ">=", "<=", "==", ">", "<"];
        string? op = null;
        var opIndex = -1;

        foreach (var o in ops)
        {
            opIndex = predicate.IndexOf($" {o} ", StringComparison.Ordinal);
            if (opIndex >= 0)
            {
                op = o;
                break;
            }
        }

        if (op is null)
            throw new ArgumentException($"No comparison operator found in: '{predicate}'");

        var propName = predicate[..opIndex].Trim();
        var valueStr = predicate[(opIndex + op.Length + 2)..].Trim().Trim('\'', '"');

        var property = GetPropertyExpression<T>(parameter, propName);
        var value = ParseValue(property.Type, valueStr);
        var valueConstant = Expression.Constant(value, property.Type);

        return op switch
        {
            "==" => Expression.Equal(property, valueConstant),
            "!=" => Expression.NotEqual(property, valueConstant),
            ">"  => Expression.GreaterThan(property, valueConstant),
            "<"  => Expression.LessThan(property, valueConstant),
            ">=" => Expression.GreaterThanOrEqual(property, valueConstant),
            "<=" => Expression.LessThanOrEqual(property, valueConstant),
            _    => throw new ArgumentException($"Unknown operator: {op}")
        };
    }

    private static Expression GetPropertyExpression<T>(ParameterExpression parameter, string propertyPath) where T : class
    {
        Expression current = parameter;

        foreach (var segment in propertyPath.Split('.'))
        {
            var prop = current.Type.GetProperty(segment,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (prop is null)
                throw new ArgumentException($"Property '{segment}' not found on type '{current.Type.Name}'");

            current = Expression.Property(current, prop);
        }

        return current;
    }

    private static object ParseValue(Type targetType, string value)
    {
        if (targetType == typeof(string))
            return value;

        if (targetType == typeof(Guid))
            return Guid.Parse(value);

        if (targetType == typeof(DateTime) || targetType == typeof(DateTimeOffset))
            return Convert.ChangeType(value, targetType);

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value, true);

        return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
    }
}
