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

    public ErrorBuilder WithCategory(ErrorCategory category)
    {
        _category = category;
        return this;
    }

    public ErrorBuilder WithTypeUri(ErrorUri? typeUri)
    {
        if (typeUri is not null)
            _typeUri = typeUri.Value;

        return this;
    }

    public ErrorBuilder WithTitle(string? title)
    {
        if (!string.IsNullOrWhiteSpace(title))
            _title = title.Trim();

        return this;
    }

    public ErrorBuilder WithDetail(string? detail)
    {
        if (!string.IsNullOrWhiteSpace(detail))
            _detail = detail.Trim();

        return this;
    }

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

    public ErrorBuilder WithDetailTemplated(string? templateId, Dictionary<string, object>? namedValues = null)
    {
        if (!string.IsNullOrWhiteSpace(templateId))
            _detailTemplateId = templateId;

        _detailParams = namedValues;
        return this;
    }

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
