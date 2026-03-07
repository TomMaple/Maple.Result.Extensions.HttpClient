using System;

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal static class ErrorUriMapper
{
    #region consts

    private const string NoneValue = "about:blank";

    #endregion

    internal static ErrorUri? Map(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        try
        {
            if (IsUriLocator(source))
                return ErrorUri.Locator(source);

            if (IsUriTag(source))
                return ErrorUri.Tag(source);
        }
        catch
        {
            // ignore
        }

        return null;
    }

    internal static ErrorUri MapTypeUri(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return ErrorUri.None();

        if (string.Equals(source, NoneValue, StringComparison.InvariantCultureIgnoreCase))
            return ErrorUri.None();

        return Map(source)
               ?? ErrorUri.None();
    }

    internal static bool IsUriLocator(string source)
    {
        return source.StartsWith("http://", StringComparison.InvariantCultureIgnoreCase) 
            || source.StartsWith("https://", StringComparison.InvariantCultureIgnoreCase);
    }

    internal static bool IsUriTag(string source)
    {
        return source.StartsWith("tag:", StringComparison.InvariantCultureIgnoreCase);
    }
}
