// SPDX-License-Identifier: MIT
/*
 * This code is a part of a Maple.Result.Extensions.HttpClient library project.
 * https://github.com/TomMaple/Maple.Result.Extensions.HttpClient
 * Copyright (c) Tom Maple
 *
 * This source code is licensed under the MIT license found in the
 * LICENSE file in the root directory of this source tree.
 */

namespace Maple.Results.Extensions.HttpClient.Tests.Unit;

// The package ID and the assembly name are Maple.Result.Extensions.HttpClient; the namespaces are Maple.Results.*,
// as in the core.
public class NamespaceUnitTests
{
    [Fact]
    public void AssemblyName_Library_IsPackageId()
    {
        // Act
        var result = typeof(HttpErrorMetadataKeys).Assembly.GetName().Name;

        // Assert
        result.ShouldBe("Maple.Result.Extensions.HttpClient");
    }

    [Fact]
    public void Namespace_LibraryType_IsMapleResultsExtensionsHttpClient()
    {
        // Act
        var result = typeof(HttpErrorMetadataKeys).Namespace;

        // Assert
        result.ShouldBe("Maple.Results.Extensions.HttpClient");
    }
}
