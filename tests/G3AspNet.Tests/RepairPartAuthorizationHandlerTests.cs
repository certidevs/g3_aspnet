using System.Security.Claims;
using G3AspNet.Authorization;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;

namespace G3AspNet.Tests;

public sealed class RepairPartAuthorizationHandlerTests
{
    [Theory]
    [InlineData(RepairStatus.Completed)]
    [InlineData(RepairStatus.Delivered)]
    public async Task ClosedRepair_AllowsPartMutationsOnlyToAdmin(RepairStatus status)
    {
        var operations = new[]
        {
            WorkshopOperations.Create,
            WorkshopOperations.Update,
            WorkshopOperations.Delete
        };

        foreach (var operation in operations)
        {
            // Admin está autorizado; la confirmación la exige el controlador.
            Assert.True(await IsAuthorizedAsync(RoleNames.Admin, status, operation));
            Assert.False(await IsAuthorizedAsync(RoleNames.Mechanic, status, operation));
        }
    }

    [Fact]
    public async Task OpenRepair_AllowsAdminAndAssignedMechanicToModifyPart()
    {
        var operations = new[]
        {
            WorkshopOperations.Create,
            WorkshopOperations.Update,
            WorkshopOperations.Delete
        };

        foreach (var role in new[] { RoleNames.Admin, RoleNames.Mechanic })
        {
            foreach (var operation in operations)
            {
                Assert.True(await IsAuthorizedAsync(role, RepairStatus.InProgress, operation));
            }
        }
    }

    [Theory]
    [InlineData(RepairStatus.Completed)]
    [InlineData(RepairStatus.Delivered)]
    public async Task ClosedRepair_StillAllowsReadingPart(RepairStatus status)
    {
        foreach (var role in new[] { RoleNames.Admin, RoleNames.Mechanic })
        {
            Assert.True(await IsAuthorizedAsync(role, status, WorkshopOperations.Read));
        }
    }

    private static async Task<bool> IsAuthorizedAsync(
        string role,
        RepairStatus status,
        OperationAuthorizationRequirement operation)
    {
        const string mechanicId = "mechanic-1";
        var repairPart = new RepairPart
        {
            Id = 1,
            Repair = new Repair
            {
                Id = 1,
                MechanicId = mechanicId,
                Status = status
            }
        };
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, role == RoleNames.Mechanic ? mechanicId : "admin-1"),
            new Claim(ClaimTypes.Role, role)
        ], "test");
        var context = new AuthorizationHandlerContext(
            [operation],
            new ClaimsPrincipal(identity),
            repairPart);
        var handler = new RepairPartAuthorizationHandler(
            NullLogger<RepairPartAuthorizationHandler>.Instance);

        await handler.HandleAsync(context);

        return context.HasSucceeded;
    }
}
