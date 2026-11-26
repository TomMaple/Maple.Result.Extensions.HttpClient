// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

using System.Text.Json.Serialization;

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.TestData;

/// <summary>
/// Source-generated metadata, as a trimmed or Native AOT application has it: only the types named here can be read.
/// </summary>
[JsonSerializable(typeof(Order))]
[JsonSerializable(typeof(ApiError))]
internal sealed partial class TestJsonContext : JsonSerializerContext
{
}
