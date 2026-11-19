using System.Security.Claims;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace G3AspNet.Authorization;

/// <summary>
/// Autorización basada en recurso para órdenes de reparación:
/// Admin tiene control total; Mechanic solo sobre sus órdenes asignadas (y no entregadas al editar);
/// Customer y User solo lectura sobre reparaciones de sus propios vehículos.
/// </summary>
public sealed class RepairAuthorizationHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, Repair>
{
    private readonly ILogger<RepairAuthorizationHandler> logger;

    public RepairAuthorizationHandler(ILogger<RepairAuthorizationHandler> logger)
    {
        this.logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Repair resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        if (context.User.IsInRole(RoleNames.Admin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.IsInRole(RoleNames.Mechanic))
        {
            var isAssigned = resource.MechanicId == userId;
            if (!isAssigned)
            {
                logger.LogWarning(
                    "Mecánico {UserId} intentó operación {Operation} sobre reparación no asignada {RepairId}.",
                    userId, requirement.Name, resource.Id);
                return Task.CompletedTask;
            }

            if (requirement.Name == WorkshopOperations.Read.Name)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if ((requirement.Name == WorkshopOperations.Update.Name ||
                 requirement.Name == WorkshopOperations.AddWork.Name) &&
                resource.Status != RepairStatus.Delivered)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            logger.LogWarning(
                "Mecánico {UserId} denegado en operación {Operation} sobre reparación {RepairId} (estado {Status}).",
                userId, requirement.Name, resource.Id, resource.Status);
            return Task.CompletedTask;
        }

        if (requirement.Name == WorkshopOperations.Read.Name &&
            resource.Vehicle is not null &&
            resource.Vehicle.OwnerId == userId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        logger.LogWarning(
            "Acceso denegado a Repair {RepairId} para usuario {UserId} en operación {Operation}.",
            resource.Id, userId, requirement.Name);

        return Task.CompletedTask;
    }
}
