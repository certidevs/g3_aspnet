using System.Security.Claims;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace G3AspNet.Authorization;

/// <summary>
/// Autorización basada en recurso para líneas de piezas usadas en una reparación.
/// </summary>
public sealed class RepairPartAuthorizationHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, RepairPart>
{
    private readonly ILogger<RepairPartAuthorizationHandler> logger;

    public RepairPartAuthorizationHandler(ILogger<RepairPartAuthorizationHandler> logger)
    {
        this.logger = logger;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        RepairPart resource)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Task.CompletedTask;
        }

        var repairIsClosed = resource.Repair?.Status is RepairStatus.Completed or RepairStatus.Delivered;
        if (repairIsClosed && requirement.Name != WorkshopOperations.Read.Name)
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
            var isAssigned = resource.Repair is not null && resource.Repair.MechanicId == userId;
            if (!isAssigned)
            {
                logger.LogWarning(
                    "Mecánico {UserId} intentó operación {Operation} sobre RepairPart {RepairPartId} de reparación ajena.",
                    userId, requirement.Name, resource.Id);
                return Task.CompletedTask;
            }

            if (requirement.Name == WorkshopOperations.Read.Name)
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            if ((requirement.Name == WorkshopOperations.Create.Name ||
                 requirement.Name == WorkshopOperations.Update.Name ||
                 requirement.Name == WorkshopOperations.Delete.Name) &&
                resource.Repair!.Status is not (RepairStatus.Completed or RepairStatus.Delivered))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }

            return Task.CompletedTask;
        }

        if (requirement.Name == WorkshopOperations.Read.Name &&
            resource.Repair?.Vehicle is not null &&
            resource.Repair.Vehicle.OwnerId == userId)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        logger.LogWarning(
            "Acceso denegado a RepairPart {RepairPartId} para usuario {UserId} en operación {Operation}.",
            resource.Id, userId, requirement.Name);

        return Task.CompletedTask;
    }
}
