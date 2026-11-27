using G3AspNet.Authorization;
using G3AspNet.Data;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using G3AspNet.Services.Invoices;
using G3AspNet.Utilities;
using G3AspNet.ViewModels.Repairs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace G3AspNet.Controllers;

[Authorize]
/// <summary>Gestiona la consulta, creación, edición y eliminación de reparaciones con autorización por recurso.</summary>
public class RepairsController : Controller
{
    private readonly ApplicationDbContext context;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IAuthorizationService authorizationService;

    public RepairsController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService)
    {
        this.context = context;
        this.userManager = userManager;
        this.authorizationService = authorizationService;
    }

    private async Task PopulateDropdownsAsync(RepairFormViewModel? repair = null)
    {
        var vehicles = await context.Vehicles
            .OrderBy(v => v.LicensePlate)
            .Select(v => new { v.Id, Description = $"{v.LicensePlate} - {v.Brand} {v.Model}" })
            .ToListAsync();
        ViewBag.Vehicles = new SelectList(vehicles, "Id", "Description", repair?.VehicleId);

        var mechanics = await userManager.GetUsersInRoleAsync(RoleNames.Mechanic);
        ViewBag.Mechanics = new SelectList(mechanics.OrderBy(m => m.DisplayName ?? m.UserName).Select(m => new
        {
            m.Id,
            Name = m.DisplayName ?? m.UserName
        }), "Id", "Name", repair?.MechanicId);
    }

    // Admin ve todas; Mechanic solo sus asignadas; Customer y User solo reparaciones de sus propios vehículos.
    private IQueryable<Repair> VisibleRepairs()
    {
        var query = context.Repairs.AsQueryable();

        if (User.IsInRole(RoleNames.Admin))
        {
            return query;
        }

        var userId = User.GetRequiredUserId();
        if (User.IsInRole(RoleNames.Mechanic))
        {
            return query.Where(r => r.MechanicId == userId);
        }

        return query.Where(r => r.Vehicle.OwnerId == userId);
    }

    /// <summary>Lista las reparaciones accesibles para el usuario actual.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var repairs = await VisibleRepairs()
            .Include(repair => repair.Vehicle)
            .Include(repair => repair.Mechanic)
            .OrderByDescending(repair => repair.OpenedAt)
            .ToListAsync();

        return View(repairs);
    }

    /// <summary>Muestra el detalle de una reparación tras validar autorización sobre el recurso.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var repair = await context.Repairs
            .Include(repair => repair.Vehicle)
            .Include(repair => repair.Mechanic)
            .Include(repair => repair.RepairParts)
            .ThenInclude(repairPart => repairPart.SparePart)
            .FirstOrDefaultAsync(repair => repair.Id == id);

        if (repair == null)
        {
            return NotFound();
        }

        var readAuth = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Read);
        if (!readAuth.Succeeded)
        {
            return Forbid();
        }

        var canEdit = (await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Update)).Succeeded;
        var canDelete = (await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Delete)).Succeeded;
        var canAddWork = (await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.AddWork)).Succeeded;

        ViewBag.CanEdit = canEdit;
        ViewBag.CanDelete = canDelete;
        ViewBag.CanAddWork = canAddWork;

        if (canAddWork)
        {
            ViewBag.AvailableSpareParts = new SelectList(
                await context.SpareParts
                    .AsNoTracking()
                    .Where(sp => sp.Stock > 0)
                    .OrderBy(sp => sp.Name)
                    .Select(sp => new
                    {
                        sp.Id,
                        Description = $"{sp.Name} - {sp.Brand} (Stock: {sp.Stock} · {sp.UnitPrice:N2} €)"
                    })
                    .ToListAsync(),
                "Id",
                "Description");
        }

        return View(repair);
    }

    /// <summary>Descarga en PDF la factura de una reparación cerrada a la que se tenga acceso.</summary>
    [HttpGet]
    public async Task<IActionResult> Invoice(int id, [FromServices] IOptions<InvoiceOptions> options)
    {
        var repair = await context.Repairs
            .AsNoTracking()
            .Include(r => r.Vehicle)
            .ThenInclude(vehicle => vehicle.Owner)
            .Include(r => r.RepairParts)
            .ThenInclude(part => part.SparePart)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (repair == null)
        {
            return NotFound();
        }

        var readAuth = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Read);
        if (!readAuth.Succeeded)
        {
            return Forbid();
        }

        if (!repair.IsClosed)
        {
            TempData["Error"] = "La factura estará disponible cuando la reparación esté completada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var invoice = Services.Invoices.Invoice.FromRepair(repair, options.Value);
        return File(InvoicePdf.Generate(invoice), "application/pdf", $"factura-{invoice.Number}.pdf");
    }

    /// <summary>Muestra el formulario para dar de alta una nueva reparación.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Create()
    {
        var model = new RepairFormViewModel
        {
            CompletedAt = null,
            MechanicId = User.IsInRole(RoleNames.Admin) ? string.Empty : User.GetRequiredUserId(),
            HourlyRate = 45m
        };
        await PopulateDropdownsAsync(model);
        return View("Form", model);
    }

    /// <summary>Registra una nueva reparación usando DTO anti-overposting.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Create(RepairFormViewModel model)
    {
        var isAdmin = User.IsInRole(RoleNames.Admin);
        if (!isAdmin)
        {
            // Un mecánico solo puede crear órdenes asignadas a sí mismo y con tarifa estándar.
            model.MechanicId = User.GetRequiredUserId();
            model.HourlyRate = 45m;
            ModelState.Remove(nameof(RepairFormViewModel.MechanicId));
            ModelState.Remove(nameof(RepairFormViewModel.HourlyRate));
        }
        else if (string.IsNullOrWhiteSpace(model.MechanicId))
        {
            ModelState.AddModelError(nameof(RepairFormViewModel.MechanicId), "Debes seleccionar un mecánico.");
        }

        if (!await context.Vehicles.AnyAsync(v => v.Id == model.VehicleId))
        {
            ModelState.AddModelError(nameof(RepairFormViewModel.VehicleId), "El vehículo seleccionado no existe.");
        }

        if (!string.IsNullOrWhiteSpace(model.MechanicId) && !await context.Users.AnyAsync(u => u.Id == model.MechanicId))
        {
            ModelState.AddModelError(nameof(RepairFormViewModel.MechanicId), "El mecánico seleccionado no existe.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View("Form", model);
        }

        var repair = new Repair
        {
            VehicleId = model.VehicleId,
            MechanicId = model.MechanicId,
            RepairType = model.RepairType,
            Status = RepairStatus.Pending,
            Description = model.Description.Trim(),
            OpenedAt = model.OpenedAt,
            CompletedAt = null,
            MileageAtEntry = model.MileageAtEntry,
            LaborHours = model.LaborHours,
            HourlyRate = model.HourlyRate
        };

        context.Repairs.Add(repair);
        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación creada correctamente.";
        return RedirectToAction(nameof(Details), new { id = repair.Id });
    }

    /// <summary>Muestra el formulario para editar una reparación existente tras validar el recurso.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Edit(int id)
    {
        var repair = await context.Repairs
            .Include(r => r.Vehicle)
            .Include(r => r.Mechanic)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (repair == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Update);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var model = new RepairFormViewModel
        {
            Id = repair.Id,
            VehicleId = repair.VehicleId,
            MechanicId = repair.MechanicId,
            RepairType = repair.RepairType,
            Status = repair.Status,
            Description = repair.Description,
            OpenedAt = repair.OpenedAt,
            CompletedAt = repair.CompletedAt,
            MileageAtEntry = repair.MileageAtEntry,
            LaborHours = repair.LaborHours,
            HourlyRate = repair.HourlyRate
        };

        ViewBag.IsClosed = repair.IsClosed;
        await PopulateDropdownsAsync(model);
        return View("Form", model);
    }

    /// <summary>Actualiza los datos permitidos de una reparación existente.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Edit(int id, RepairFormViewModel model, bool confirmClosed = false)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var repair = await context.Repairs.FindAsync(id);
        if (repair == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Update);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var isAdmin = User.IsInRole(RoleNames.Admin);
        if (!isAdmin)
        {
            // Protege campos reservados a Admin frente a overposting (API3:2023).
            model.MechanicId = repair.MechanicId;
            model.HourlyRate = repair.HourlyRate;
            ModelState.Remove(nameof(RepairFormViewModel.MechanicId));
            ModelState.Remove(nameof(RepairFormViewModel.HourlyRate));
        }
        else if (string.IsNullOrWhiteSpace(model.MechanicId))
        {
            ModelState.AddModelError(nameof(RepairFormViewModel.MechanicId), "Debes seleccionar un mecánico.");
        }

        if (!await context.Vehicles.AnyAsync(v => v.Id == model.VehicleId))
        {
            ModelState.AddModelError(nameof(RepairFormViewModel.VehicleId), "El vehículo seleccionado no existe.");
        }

        if (repair.IsClosed && !confirmClosed)
        {
            ModelState.AddModelError(string.Empty, ClosedRepairConfirmation.Required);
        }

        if (!ModelState.IsValid)
        {
            ViewBag.IsClosed = repair.IsClosed;
            model.Status = repair.Status;
            model.CompletedAt = repair.CompletedAt;
            await PopulateDropdownsAsync(model);
            return View("Form", model);
        }

        repair.VehicleId = model.VehicleId;
        repair.RepairType = model.RepairType;
        repair.OpenedAt = model.OpenedAt;
        repair.MileageAtEntry = model.MileageAtEntry;
        repair.LaborHours = model.LaborHours;
        repair.Description = model.Description.Trim();

        if (isAdmin)
        {
            repair.MechanicId = model.MechanicId;
            repair.HourlyRate = model.HourlyRate;
        }

        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación actualizada correctamente.";
        return RedirectToAction(nameof(Details), new { id = repair.Id });
    }

    /// <summary>Muestra la confirmación para eliminar una reparación (solo Admin).</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.SoloAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var repair = await context.Repairs
            .Include(r => r.Vehicle)
            .Include(r => r.Mechanic)
            .Include(r => r.RepairParts)
            .ThenInclude(rp => rp.SparePart)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (repair == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Delete);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        return View(repair);
    }

    /// <summary>Elimina la reparación y devuelve al inventario el stock de las piezas asociadas (solo Admin).</summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.SoloAdmin)]
    public async Task<IActionResult> DeleteConfirmed(int id, bool confirmClosed = false)
    {
        var repair = await context.Repairs
            .Include(r => r.RepairParts)
            .ThenInclude(rp => rp.SparePart)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (repair == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Delete);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        if (repair.IsClosed && !confirmClosed)
        {
            TempData["Error"] = ClosedRepairConfirmation.Required;
            return RedirectToAction(nameof(Delete), new { id });
        }

        foreach (var part in repair.RepairParts)
        {
            if (part.SparePart != null)
            {
                part.SparePart.Stock += part.Quantity;
            }
        }

        context.Repairs.Remove(repair);
        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Start(int id)
    {
        var repair = await context.Repairs.FirstOrDefaultAsync(r => r.Id == id);
        if (repair == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Update);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        if (repair.Status != RepairStatus.Pending && repair.Status != RepairStatus.WaitingParts)
        {
            TempData["Error"] = "Solo se puede iniciar una reparación pendiente o retomar una que espera recambios.";
            return RedirectToAction(nameof(Details), new { id });
        }

        repair.Status = RepairStatus.InProgress;
        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación en progreso.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> WaitForParts(int id)
    {
        var repair = await context.Repairs.FirstOrDefaultAsync(r => r.Id == id);
        if (repair == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Update);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        if (repair.Status != RepairStatus.InProgress)
        {
            TempData["Error"] = "Solo se puede esperar recambios en una reparación en progreso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        repair.Status = RepairStatus.WaitingParts;
        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación en espera de recambios.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Complete(int id)
    {
        var repair = await context.Repairs
            .Include(r => r.RepairParts)
            .FirstOrDefaultAsync(r => r.Id == id);
        if (repair == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.Update);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        if (repair.Status != RepairStatus.InProgress)
        {
            TempData["Error"] = "Solo se puede completar una reparación en progreso.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (repair.LaborHours <= 0)
        {
            TempData["Error"] = "Apunta las horas de mano de obra antes de completar la reparación.";
            return RedirectToAction(nameof(Details), new { id });
        }

        repair.Status = RepairStatus.Completed;
        repair.CompletedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();

        TempData["Message"] = $"Reparación completada. Coste total: {repair.TotalCost:N2} €.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.SoloAdmin)]
    public async Task<IActionResult> Deliver(int id)
    {
        var repair = await context.Repairs.FirstOrDefaultAsync(r => r.Id == id);
        if (repair == null)
        {
            return NotFound();
        }

        if (repair.Status != RepairStatus.Completed)
        {
            TempData["Error"] = "Solo se puede entregar una reparación completada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        repair.Status = RepairStatus.Delivered;
        await context.SaveChangesAsync();

        TempData["Message"] = "Vehículo entregado al cliente. La reparación queda cerrada.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Añade un recambio a la reparación autorizada y descuenta la cantidad del stock disponible.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> AddPart(int id, int sparePartId, int quantity, bool confirmClosed = false)
    {
        var repair = await context.Repairs
            .Include(r => r.RepairParts)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (repair == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.AddWork);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        if (repair.IsClosed && !confirmClosed)
        {
            TempData["Error"] = ClosedRepairConfirmation.Required;
            return RedirectToAction(nameof(Details), new { id });
        }

        if (sparePartId <= 0)
        {
            TempData["Error"] = "Debes seleccionar un recambio válido.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var sparePart = await context.SpareParts.FindAsync(sparePartId);
        if (sparePart == null)
        {
            return NotFound();
        }

        try
        {
            repair.AddPart(sparePart, quantity);
            await context.SaveChangesAsync();
            TempData["Message"] = "Pieza añadida correctamente.";
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Quita una pieza asignada a la reparación autorizada y devuelve las unidades al stock.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> RemovePart(int id, int repairPartId, bool confirmClosed = false)
    {
        var repairPart = await context.RepairParts
            .Include(rp => rp.Repair)
            .Include(rp => rp.SparePart)
            .FirstOrDefaultAsync(rp => rp.Id == repairPartId && rp.RepairId == id);

        if (repairPart == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repairPart.Repair, WorkshopOperations.AddWork);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        if (repairPart.Repair.IsClosed && !confirmClosed)
        {
            TempData["Error"] = ClosedRepairConfirmation.Required;
            return RedirectToAction(nameof(Details), new { id });
        }

        repairPart.SparePart.Stock += repairPart.Quantity;
        context.RepairParts.Remove(repairPart);
        await context.SaveChangesAsync();

        TempData["Message"] = "Pieza retirada de la reparación y stock restablecido.";
        return RedirectToAction(nameof(Details), new { id });
    }
}