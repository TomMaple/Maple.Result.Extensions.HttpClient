// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Collections.Generic;
using System.Linq;

namespace Maple.Result.Extensions.HttpClient.Helpers;

/// <summary>
///     Creates an <see cref="Error" /> instance using a fluent API. This builder allows you to set various properties
///     of the error without the need of using factory methods for each error category.
/// </summary>
public class ErrorBuilder
{
    #region fields

    private ErrorCategory _category;
    private ErrorUri _typeUri = ErrorUri.None();
    private string _title;
    private string? _detail;
    private string? _detailTemplateId;
    private Dictionary<string, object>? _detailParams;
    private ErrorUri? _instanceUri;
    private List<ErrorDetail>? _errorDetails;

    #endregion

    #region constructors

    internal ErrorBuilder(ErrorCategory category, string title)
    {
        _category = category;
        _title = title.Trim();
    }

    #endregion

    /// <summary>
    ///     Sets the <see cref="Error.Category" /> for the current <see cref="Error" /> instance to be built.
    /// </summary>
    /// <remarks>
    ///     Use this method to specify the category of the error when building a custom error.
    ///     This enables fluent configuration of error details.
    /// </remarks>
    /// <param name="category">The error category to associate with the error being built.</param>
    /// <returns>The current <see cref="ErrorBuilder" /> instance with the specified category applied.</returns>
    public ErrorBuilder WithCategory(ErrorCategory category)
    {
        _category = category;
        return this;
    }

    /// <summary>
    ///     Sets the <see cref="Error.TypeUri" /> for the current <see cref="Error" /> instance to be built.
    /// </summary>
    /// <param name="typeUri">
    ///     The type URI to associate with the error, or
    ///     <see langword="null" /> to leave the current value unchanged.
    /// </param>
    /// <returns>The current <see cref="ErrorBuilder" /> instance with the updated type URI.</returns>
    public ErrorBuilder WithTypeUri(ErrorUri? typeUri)
    {
        if (typeUri is not null)
            _typeUri = typeUri.Value;

        return this;
    }

    /// <summary>
    ///     Sets the <see cref="Error.Title" /> for the current <see cref="Error" /> instance to be built.
    /// </summary>
    /// <param name="title">
    ///     The title to associate with the error, or
    ///     <see langword="null" /> to leave the current value unchanged.
    /// </param>
    /// <returns>The current <see cref="ErrorBuilder" /> instance with the updated title.</returns>
    public ErrorBuilder WithTitle(string? title)
    {
        if (!string.IsNullOrWhiteSpace(title))
            _title = title.Trim();

        return this;
    }

    /// <summary>
    ///     Sets the <see cref="Error.Detail" /> for the current <see cref="Error" /> instance to be built.
    /// </summary>
    /// <param name="detail">
    ///     The detail to associate with the error, or
    ///     <see langword="null" /> to leave the current value unchanged.
    /// </param>
    /// <returns>The current <see cref="ErrorBuilder" /> instance with the updated detail.</returns>
    public ErrorBuilder WithDetail(string? detail)
    {
        if (!string.IsNullOrWhiteSpace(detail))
            _detail = detail.Trim();

        return this;
    }

    /// <summary>
    ///     Sets the <see cref="Error.DetailTemplated" /> for the current <see cref="Error" /> instance to be built.
    /// </summary>
    /// <param name="detailTemplated">
    ///     The templated detail to associate with the error, or
    ///     <see langword="null" /> to leave the current value unchanged.
    /// </param>
    /// <returns>The current <see cref="ErrorBuilder" /> instance with the updated templated detail.</returns>
    public ErrorBuilder WithDetailTemplated(TemplatedMessage? detailTemplated)
    {
        if (!string.IsNullOrWhiteSpace(detailTemplated?.TemplateId))
            _detailTemplateId = detailTemplated.TemplateId;

        if (detailTemplated?.Params is {Count:>0})
        {
            _detailParams = detailTemplated.Params as Dictionary<string, object>
                            ?? detailTemplated.Params.ToDictionary();
        }

        return this;
    }

    /// <summary>
    ///     Sets the <see cref="Error.DetailTemplated" /> for the current <see cref="Error" /> instance to be built.
    /// </summary>
    /// <param name="templateId">The ID of the template to generate localized error detail.</param>
    /// <param name="namedValues">The map of parameters to generate localized error detail.</param>
    /// <returns>The current <see cref="ErrorBuilder" /> instance with the updated templated detail.</returns>
    public ErrorBuilder WithDetailTemplated(string? templateId, Dictionary<string, object>? namedValues = null)
    {
        if (!string.IsNullOrWhiteSpace(templateId))
            _detailTemplateId = templateId;

        _detailParams = namedValues;
        return this;
    }

    /// <summary>
    ///     Sets the <see cref="Error.InstanceUri" /> for the current <see cref="Error" /> instance to be built.
    /// </summary>
    /// <param name="instanceUri">
    ///     The detail to associate with the error, or
    ///     <see langword="null" /> to leave the current value unchanged.
    /// </param>
    /// <returns>The current <see cref="ErrorBuilder" /> instance with the updated instance URI.</returns>
    public ErrorBuilder WithInstanceUri(ErrorUri? instanceUri)
    {
        if (instanceUri is not null)
            _instanceUri = instanceUri;

        return this;
    }

    internal ErrorBuilder WithErrorDetail(ErrorDetail? errorDetail)
    {
        if (errorDetail is null)
            return this;

        _errorDetails ??= [];
        _errorDetails.Add(errorDetail);

        return this;
    }

    /// <summary>
    ///     Adds an <see cref="Error.ErrorDetails" /> for the current <see cref="Error" /> instance to be built.
    /// </summary>
    /// <param name="propertyPointer">
    ///     The JSON Pointer indicating the property of the input value that the <paramref name="detail"/>
    ///     relates to.
    /// </param>
    /// <param name="detail">The error detail for that specific to associate with the error detail.</param>
    /// <param name="messageId">The ID of the template to generate localized error detail.</param>
    /// <param name="namedValues">The map of parameters to generate localized error detail.</param>
    /// <returns>The current <see cref="ErrorBuilder" /> instance with the updated templated detail.</returns>
    public ErrorBuilder WithErrorDetail(string? propertyPointer, string? detail, string? messageId = null,
        params (string key, object value)[] namedValues)
    {
        if (string.IsNullOrWhiteSpace(detail)
            && string.IsNullOrWhiteSpace(propertyPointer)
            && string.IsNullOrWhiteSpace(messageId)
            && namedValues is not {Length:>0})
        {
            return this;
        }
        
        var detailValue = detail?.Trim() ?? string.Empty;
        var detailTemplated = string.IsNullOrWhiteSpace(messageId)
            ? null
            : new TemplatedMessage(messageId, namedValues.ToDictionary(x => x.key, x => x.value));

        _errorDetails ??= [];
        _errorDetails.Add(new ErrorDetail(propertyPointer, detailValue, detailTemplated));

        return this;
    }

    internal Error Build()
    {
        var error = _category switch
        {
            ErrorCategory.Validation => Error.Validation(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams),
            ErrorCategory.Unauthenticated => Error.Unauthenticated(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams),
            ErrorCategory.Unauthorized => Error.Unauthorized(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams),
            ErrorCategory.NotFound => Error.NotFound(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams),
            ErrorCategory.Timeout => Error.Timeout(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams),
            ErrorCategory.Conflict => Error.Conflict(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams),
            ErrorCategory.Failure => Error.Failure(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams),
            ErrorCategory.Critical => Error.Critical(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams),
            ErrorCategory.NotImplemented => Error.NotImplemented(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams),
            ErrorCategory.Unavailable => Error.Unavailable(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams),
            _ => Error.Failure(_typeUri, _title, _detail, _instanceUri, _detailTemplateId, _detailParams)
        };

        if (_errorDetails is not null)
        {
            foreach (var errorDetail in _errorDetails)
                error.AddDetail(errorDetail);
        }

        return error;
    }
}
