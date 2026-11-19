using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace G3AspNet.Authorization;

/// <summary>Operaciones evaluadas por los AuthorizationHandlers de recursos del taller.</summary>
public static class WorkshopOperations
{
    public static readonly OperationAuthorizationRequirement Read =
        new() { Name = nameof(Read) };

    public static readonly OperationAuthorizationRequirement Create =
        new() { Name = nameof(Create) };

    public static readonly OperationAuthorizationRequirement Update =
        new() { Name = nameof(Update) };

    public static readonly OperationAuthorizationRequirement Delete =
        new() { Name = nameof(Delete) };

    public static readonly OperationAuthorizationRequirement AddWork =
        new() { Name = nameof(AddWork) };

    public static readonly OperationAuthorizationRequirement ConfigurePricing =
        new() { Name = nameof(ConfigurePricing) };
}
