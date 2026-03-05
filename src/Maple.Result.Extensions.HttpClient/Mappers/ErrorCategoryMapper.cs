using System.Net;

namespace Maple.Result.Extensions.HttpClient.Mappers;

internal class ErrorCategoryMapper
{
    internal static ErrorCategory Map(HttpStatusCode statusCode)
    {
        return statusCode switch
        {
            HttpStatusCode.BadRequest => ErrorCategory.Validation,
            HttpStatusCode.Unauthorized => ErrorCategory.Unauthenticated,
            HttpStatusCode.Forbidden => ErrorCategory.Unauthorized,
            HttpStatusCode.NotFound => ErrorCategory.NotFound,
            HttpStatusCode.RequestTimeout or HttpStatusCode.GatewayTimeout => ErrorCategory.Timeout,
            HttpStatusCode.Conflict => ErrorCategory.Conflict,
            HttpStatusCode.UnprocessableContent => ErrorCategory.Failure,
            HttpStatusCode.InternalServerError => ErrorCategory.Critical,
            HttpStatusCode.NotImplemented => ErrorCategory.NotImplemented,
            HttpStatusCode.ServiceUnavailable => ErrorCategory.Unavailable,
            _ => ErrorCategory.Failure
        };
    }
}
