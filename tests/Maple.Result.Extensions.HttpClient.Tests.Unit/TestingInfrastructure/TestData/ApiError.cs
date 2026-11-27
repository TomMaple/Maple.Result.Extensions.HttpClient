// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Maple.Results.Extensions.HttpClient.Tests.Unit.TestingInfrastructure.TestData;

/// <summary>
/// The body of an error response of a service that does not answer with problem details.
/// </summary>
internal sealed record ApiError(string? Code, string? Message);
