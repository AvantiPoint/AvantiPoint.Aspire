using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;

namespace AvantiPoint.Aspire.Cloudflare.D1;

/// <summary>
/// Maps normalized <see cref="D1Row"/> values to CLR types. Shared by both backends so a POCO or scalar
/// materializes identically whether the row came from SQLite or the D1 HTTP API.
/// </summary>
internal static class D1RowMapper
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> PropertyCache = new();

    /// <summary>Maps a row to <typeparamref name="T"/>: a <see cref="D1Row"/>, a dictionary, a scalar, or a POCO.</summary>
    public static T Map<T>(D1Row row)
    {
        var target = typeof(T);

        if (target == typeof(D1Row))
        {
            return (T)(object)row;
        }

        if (target == typeof(Dictionary<string, object?>) || target == typeof(IReadOnlyDictionary<string, object?>))
        {
            return (T)(object)new Dictionary<string, object?>(row.Values, StringComparer.OrdinalIgnoreCase);
        }

        if (IsScalar(target))
        {
            // Scalar query (e.g. SELECT COUNT(*)): take the first column value.
            var first = row.Values.Count > 0 ? row.Values.Values.First() : null;
            return ConvertValue<T>(first)!;
        }

        return MapPoco<T>(row);
    }

    private static T MapPoco<T>(D1Row row)
    {
        var instance = Activator.CreateInstance<T>();
        var properties = PropertyCache.GetOrAdd(typeof(T), static t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite)
                .ToArray());

        foreach (var property in properties)
        {
            if (!TryGetIgnoreCase(row.Values, property.Name, out var raw))
            {
                continue;
            }

            var converted = ConvertValue(property.PropertyType, raw);
            if (converted is not null || IsNullable(property.PropertyType))
            {
                property.SetValue(instance, converted);
            }
        }

        return instance;
    }

    public static T? ConvertValue<T>(object? value) => (T?)ConvertValue(typeof(T), value);

    public static object? ConvertValue(Type targetType, object? value)
    {
        if (value is null || value is DBNull)
        {
            return null;
        }

        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (underlying.IsInstanceOfType(value))
        {
            return value;
        }

        if (underlying == typeof(string))
        {
            return value as string ?? Convert.ToString(value, CultureInfo.InvariantCulture);
        }

        if (underlying == typeof(bool))
        {
            // SQLite/D1 store booleans as integers.
            return value switch
            {
                long l => l != 0,
                int i => i != 0,
                double d => d != 0,
                string s => bool.TryParse(s, out var b) ? b : s is "1",
                _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture),
            };
        }

        if (underlying == typeof(Guid))
        {
            return value is string g ? Guid.Parse(g) : value;
        }

        if (underlying == typeof(DateTime))
        {
            return value is string dt
                ? DateTime.Parse(dt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                : Convert.ToDateTime(value, CultureInfo.InvariantCulture);
        }

        if (underlying == typeof(DateTimeOffset))
        {
            return value is string dto
                ? DateTimeOffset.Parse(dto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
                : value;
        }

        if (underlying == typeof(byte[]))
        {
            return value is string b64 ? Convert.FromBase64String(b64) : value;
        }

        if (underlying.IsEnum)
        {
            return value is string es
                ? Enum.Parse(underlying, es, ignoreCase: true)
                : Enum.ToObject(underlying, Convert.ToInt64(value, CultureInfo.InvariantCulture));
        }

        try
        {
            return Convert.ChangeType(value, underlying, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is InvalidCastException or FormatException or OverflowException)
        {
            return value;
        }
    }

    private static bool TryGetIgnoreCase(IReadOnlyDictionary<string, object?> values, string key, out object? value)
    {
        if (values.TryGetValue(key, out value))
        {
            return true;
        }

        foreach (var pair in values)
        {
            if (string.Equals(pair.Key, key, StringComparison.OrdinalIgnoreCase))
            {
                value = pair.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static bool IsNullable(Type type) => !type.IsValueType || Nullable.GetUnderlyingType(type) is not null;

    private static bool IsScalar(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive
            || underlying.IsEnum
            || underlying == typeof(string)
            || underlying == typeof(decimal)
            || underlying == typeof(Guid)
            || underlying == typeof(DateTime)
            || underlying == typeof(DateTimeOffset)
            || underlying == typeof(byte[]);
    }
}
