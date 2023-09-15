using System.Text;

namespace ContentNegotiationDemo;

// Copied from https://stackoverflow.com/a/72553753
public static class CsvHelper
{
    /// <summary>
    /// Converts the given enumerable into a CSV string. Optionally, specify the delimiter or include headers.
    /// For enumerables of primitive types, it will convert them to a single-line CSV. Headers are not valid for this case.
    /// For enumerables of complex types, it will inspect the properties and convert each item into a line of the CSV.
    /// Which properties are included/excluded and the header names in the resulting CSV can be controlled.
    /// Note: Headers and values will only be double-quoted if necessary as per RFC4180.
    /// </summary>
    /// <typeparam name="T">The type of the enumerable.</typeparam>
    /// <param name="enumerable">The enumerable to turn into a CSV.</param>
    /// <param name="delimiter">The delimiter.</param>
    /// <param name="includeHeaders">Whether to include headers.</param>
    /// <param name="propertiesToInclude">Properties from the objects given to include. If left null, all properties will be included. This does not apply for enumerables of primitive types.</param>
    /// <param name="propertiesToExclude">Properties to exclude from the DataTable, if any. This does not apply for enumerables of primitive types.</param>
    /// <param name="propertyNameHeaderMap">A map that will be used to translate the property names to the headers that should appear in the CSV. This does not apply for enumerables of primitive types.</param>
    /// <returns>A CSV representation of the objects in the enumeration.</returns>
    public static string ToCsvString<T>(
        this IEnumerable<T> enumerable,
        char delimiter = ',',
        bool includeHeaders = false,
        IEnumerable<string> propertiesToInclude = null,
        IEnumerable<string> propertiesToExclude = null,
        Dictionary<string, string> propertyNameHeaderMap = null)
    {
        if (enumerable == null) throw new ArgumentNullException(nameof(enumerable));

        var type = enumerable.FirstOrDefault()?.GetType();
        if (type == null) return "";

        if (type.IsSimpleType())
            return string.Join(delimiter, enumerable.Select(i => escapeCsvValue(i?.ToString(), delimiter)));

        var csvBuilder = new StringBuilder();
        var allProperties = type.GetProperties();
        var propsToIncludeSet = (propertiesToInclude ?? allProperties.Select(p => p.Name))
            .Except(propertiesToExclude ?? Enumerable.Empty<string>())
            .ToHashSet();
        var properties = allProperties
            .Where(p => propsToIncludeSet.Contains(p.Name))
            .ToList();

        if (includeHeaders)
        {
            var headerNames = properties
                .Select(p => escapeCsvValue(propertyNameHeaderMap == null ? p.Name : propertyNameHeaderMap.GetValueOrDefault(p.Name) ?? $"{nameof(propertyNameHeaderMap)} was missing a value for property {p.Name}", delimiter));

            csvBuilder.AppendLine(string.Join(delimiter, headerNames));
        }

        foreach (var item in enumerable)
        {
            var vals = properties.Select(p => escapeCsvValue(p.GetValue(item, null)?.ToString(), delimiter));
            var line = string.Join(delimiter, vals);
            csvBuilder.AppendLine(line);
        }
        return csvBuilder.ToString();

        //Function to escape a value for use in CSV. Per RFC4180, if the delimiter, newline, or double quote is present in the value it must be double quoted. If the value contains double quotes they must be escaped.
        static string escapeCsvValue(string s, char delimiter)
        {
            return s == null ? null
                : s.Any(c => c == delimiter || c == '"' || c == '\r' || c == '\n') ? $"\"{s.Replace("\"", "\"\"")}\""
                : s;
        }
    }

    /// <summary>
    /// Whether the given type is a "simple" type. Eg, a built in CLR type to represent data.
    /// This includes all integral types, floating points types, DateTime, DateOnly, decimal, and Guid.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="unwrapNullable">Whether the type inside a nullable type should be checked.</param>
    /// <returns>Whether the type was a simple type.</returns>
    /// <exception cref="ArgumentNullException">If type was empty.</exception>
    public static bool IsSimpleType(this Type type, bool unwrapNullable = true)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));

        if (unwrapNullable) type = Nullable.GetUnderlyingType(type) ?? type;
        return type.IsPrimitive
               || type == typeof(string)
               || type == typeof(DateTime)
               || type == typeof(DateOnly)
               || type == typeof(decimal)
               || type == typeof(Guid)
            ;
    }
}
