namespace Maple.Result.Extensions.HttpClient.Extensions;

internal static class DictionaryExtensions
{
    internal static object? GetValueOrNull(this IDictionary<string, object?>? dictionary, string key)
    {
        if (dictionary is null)
            return null;

        var existingKey = dictionary.Keys.FirstOrDefault(k => string.Equals(k, key, StringComparison.InvariantCultureIgnoreCase));

        return existingKey is not null 
            ? dictionary[existingKey] 
            : null;
    }
}
