# Maple.Result.Extensions.HttpClient
The extension methods that map [HttpClient](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient) result to [Maple.Result](https://github.com/TomMaple/Result).


# Requirements
.NET 10 or later. Version 3.0 dropped .NET 8.

# Project status: EARLY STAGE

Version 3.0 is a rewrite for [Maple.Result](https://github.com/TomMaple/Maple.Result) 3.0, and it is in progress. The `main` branch has the new `ToResultAsync` API, which is not released yet and does not read problem details yet (see *Status* below); the latest package on NuGet is [0.12.11](https://www.nuget.org/packages/Maple.Result.Extensions.HttpClient/0.12.11), from before the rewrite.

Check also my other libraries:
* [Maple.Result](https://github.com/TomMaple/Result),
* [Maple.Result.Extensions.AspNetCore](https://github.com/TomMaple/Maple.Result.Extensions.AspNetCore),
* *Maple.Result.Extensions.Functions.Worker* (coming soon).

# Give it a star ⭐
Do you like it? Show your support by giving this project a star!

# Status
The steps of the 3.0 rewrite:

✅ Maple.Result 3.0 and the removal of the old implementation ([#27](https://github.com/TomMaple/Maple.Result.Extensions.HttpClient/issues/27))  
✅ `ToResultAsync` with options, an error context and custom mappers ([#28](https://github.com/TomMaple/Maple.Result.Extensions.HttpClient/issues/28))  
🔲 Reading RFC 9457 problem details ([#29](https://github.com/TomMaple/Maple.Result.Extensions.HttpClient/issues/29))  
🔲 Native AOT ([#17](https://github.com/TomMaple/Maple.Result.Extensions.HttpClient/issues/17))  
🔲 `IHttpClientBuilder.AddMapleResult` for clients of `IHttpClientFactory` ([#30](https://github.com/TomMaple/Maple.Result.Extensions.HttpClient/issues/30))  
🔲 The `Retry-After` header as a retry hint ([#31](https://github.com/TomMaple/Maple.Result.Extensions.HttpClient/issues/31))  
🔲 Documentation ([#32](https://github.com/TomMaple/Maple.Result.Extensions.HttpClient/issues/32))  

## See also
* [Problem Details for HTTP APIs - RFC 7807 is dead, long live RFC 9457](https://blog.frankel.ch/problem-details-http-apis/)
* [tag URI scheme](https://en.wikipedia.org/wiki/Tag_URI_scheme)
* [RFC 1738: Uniform Resource Locators (URL)](https://datatracker.ietf.org/doc/html/rfc1738)
* [RFC 3986: Uniform Resource Identifier (URI): Generic Syntax](https://datatracker.ietf.org/doc/html/rfc3986)
* [RFC 4151: The 'tag' URI Scheme](https://datatracker.ietf.org/doc/html/rfc4151)
* [RFC 6901: JavaScript Object Notation (JSON) Pointer](https://datatracker.ietf.org/doc/html/rfc6901)
* [RFC 9457: Problem Details for HTTP APIs](https://datatracker.ietf.org/doc/html/rfc9457)

# Contribution
Please contact author: engineer(at sign)blumail(dot)me