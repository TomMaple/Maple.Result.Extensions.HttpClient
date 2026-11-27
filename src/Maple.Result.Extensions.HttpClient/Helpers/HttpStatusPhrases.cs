// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using Maple.Results.Http;
using System.Net;

namespace Maple.Results.Extensions.HttpClient.Helpers;

/// <summary>
///     The phrases of the HTTP status codes, for the title of an error whose response does not give one.
/// </summary>
/// <remarks>
///     <para>
///         The table is fixed: it has the codes of <i>RFC 9110</i> and of the HTTP status code registry of IANA, with their names there
///         (e.g., “Content Too Large” for 413). The reason phrase of the response is not used: HTTP/2 and HTTP/3 have none, and a server
///         can send any text.
///     </para>
///     <para>
///         The phrases of the status codes that the categories of Maple.Result write by default are the constants of
///         <see cref="HttpErrorTitles" />, so the title of an error written by another package of Maple.Result is read back unchanged.
///     </para>
/// </remarks>
internal static class HttpStatusPhrases
{
    /// <summary>
    ///     The phrase of a status code that the table does not have.
    /// </summary>
    internal const string Unknown = "Unknown Error";

    /// <summary>
    ///     Returns the phrase of the <paramref name="statusCode" />, or <see cref="Unknown" /> for a code that is not registered.
    /// </summary>
    internal static string Get(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;

        return code switch
        {
            100 => "Continue",
            101 => "Switching Protocols",
            102 => "Processing",
            103 => "Early Hints",
            200 => "OK",
            201 => "Created",
            202 => "Accepted",
            203 => "Non-Authoritative Information",
            204 => "No Content",
            205 => "Reset Content",
            206 => "Partial Content",
            207 => "Multi-Status",
            208 => "Already Reported",
            226 => "IM Used",
            300 => "Multiple Choices",
            301 => "Moved Permanently",
            302 => "Found",
            303 => "See Other",
            304 => "Not Modified",
            305 => "Use Proxy",
            307 => "Temporary Redirect",
            308 => "Permanent Redirect",
            400 => HttpErrorTitles.Validation,
            401 => HttpErrorTitles.Unauthenticated,
            402 => "Payment Required",
            403 => HttpErrorTitles.PermissionDenied,
            404 => HttpErrorTitles.NotFound,
            405 => "Method Not Allowed",
            406 => "Not Acceptable",
            407 => "Proxy Authentication Required",
            408 => HttpErrorTitles.Timeout,
            409 => HttpErrorTitles.Conflict,
            410 => "Gone",
            411 => "Length Required",
            412 => "Precondition Failed",
            413 => "Content Too Large",
            414 => "URI Too Long",
            415 => "Unsupported Media Type",
            416 => "Range Not Satisfiable",
            417 => "Expectation Failed",
            421 => "Misdirected Request",
            422 => HttpErrorTitles.Failure,
            423 => "Locked",
            424 => "Failed Dependency",
            425 => "Too Early",
            426 => "Upgrade Required",
            428 => "Precondition Required",
            429 => HttpErrorTitles.LimitExceeded,
            431 => "Request Header Fields Too Large",
            451 => "Unavailable For Legal Reasons",
            500 => HttpErrorTitles.Critical,
            501 => HttpErrorTitles.NotImplemented,
            502 => "Bad Gateway",
            503 => HttpErrorTitles.Unavailable,
            504 => "Gateway Timeout",
            505 => "HTTP Version Not Supported",
            506 => "Variant Also Negotiates",
            507 => "Insufficient Storage",
            508 => "Loop Detected",
            510 => "Not Extended",
            511 => "Network Authentication Required",
            _ => Unknown
        };
    }
}
