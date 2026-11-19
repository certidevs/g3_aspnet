using System.Security.Claims;
using G3AspNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace G3AspNet.Authorization;

/// <summary>Autorización basada en recurso para vehículos: Admin y Mechanic gestionan; Customer/User solo leen los suyos.</summary>
public sealed class VehicleAuthorizationHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, Vehicle>
{
    private readonly ILogger<VehicleAuthorizationHandler> logger;

    public VehicleAuthorizationHandler(ILogger<VehicleAuthorizationHandler> logger)
    {
        this.logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Vehicle resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        if (context.User.IsInRole(RoleNames.Admin) || context.User.IsInRole(RoleNames.Mechanic))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (requirement.Name == WorkshopOperations.Read.Name && resource.OwnerId == userId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        logger.LogWarning(
            "Acceso denegado a Vehicle {VehicleId} para usuario {UserId} en operación {Operation}.",
            resource.Id, userId, requirement.Name);

        return Task.CompletedTask;
    }
}
