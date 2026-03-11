// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System;
using System.Collections.Generic;
using System.Linq;

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
