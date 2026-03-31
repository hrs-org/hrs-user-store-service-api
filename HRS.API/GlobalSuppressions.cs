using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Performance",
    "CA1812:Avoid uninstantiated internal classes",
    Justification = "False positive from stale design-time analyzer cache after refactor; symbol no longer exists in source.",
    Scope = "type",
    Target = "~T:HRS.API.Services.Auth0ManagementService.Auth0TokenResponse")]
