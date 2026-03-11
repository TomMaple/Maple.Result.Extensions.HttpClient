using System.Net;
using System.Text.RegularExpressions;

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal static partial class ErrorTitleMapper
{
    #region consts

    [GeneratedRegex("(?<!^)([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex PascalCaseRegex();

    #endregion


    internal static string Map(string? reasonPhrase, HttpStatusCode statusCode)
    {
        if (!string.IsNullOrWhiteSpace(reasonPhrase))
            return reasonPhrase.Trim();

        return GetDefaultTitle(statusCode);
    }

    private static string GetDefaultTitle(HttpStatusCode statusCode)
    {
        var enumDescription = statusCode.ToString();

        return PascalCaseToTitleCase(enumDescription);
    }

    private static string PascalCaseToTitleCase(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        return PascalCaseRegex().Replace(input, " $1");
    }
}
