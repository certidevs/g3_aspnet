using G3AspNet.Authorization;
using G3AspNet.Data;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using G3AspNet.Utilities;
using G3AspNet.ViewModels.RepairParts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

[Authorize]
public class RepairPartsController : Controller
{
    private readonly ApplicationDbContext context;
    private readonly IAuthorizationService authorizationService;

    /// <summary>Inicializa el controlador con el contexto de la base de datos y autorización.</summary>
    public RepairPartsController(
        ApplicationDbContext context,
        IAuthorizationService authorizationService)
    {
        this.context = context;
        this.authorizationService = authorizationService;
    }

    // Admin ve todas; Mechanic solo piezas de sus reparaciones asignadas; Customer y User solo de sus vehículos.
    private IQueryable<RepairPart> VisibleRepairParts()
    {
        var query = context.RepairParts.AsQueryable();

        if (User.IsInRole(RoleNames.Admin))
        {
            return query;
        }

        var userId = User.GetRequiredUserId();
        if (User.IsInRole(RoleNames.Mechanic))
        {
            return query.Where(rp => rp.Repair.MechanicId == userId);
        }

        return query.Where(rp => rp.Repair.Vehicle.OwnerId == userId);
    }

    /// <summary>Prepara las listas de reparaciones y recambios para los formularios.</summary>
    private async Task PopulateDropdownsAsync(RepairPartFormViewModel? repairPart = null)
    {
        var repairsQuery = context.Repairs.AsQueryable();
        if (!User.IsInRole(RoleNames.Admin))
        {
            var userId = User.GetRequiredUserId();
            repairsQuery = repairsQuery.Where(r => r.MechanicId == userId && r.Status != RepairStatus.Delivered);
        }

        var repairs = await repairsQuery
            .OrderBy(repair => repair.Id)
            .Select(repair => new
            {
                repair.Id,
                Description = $"#{repair.Id} - {repair.Vehicle!.LicensePlate}"
            })
            .ToListAsync();
        ViewBag.Repairs = new SelectList(repairs, "Id", "Description", repairPart?.RepairId);

        var spareParts = await context.SpareParts
            .OrderBy(sparePart => sparePart.Name)
            .Select(sparePart => new
            {
                sparePart.Id,
                Description = $"{sparePart.Name} - {sparePart.Brand}"
            })
            .ToListAsync();
        ViewBag.SpareParts = new SelectList(spareParts, "Id", "Description", repairPart?.SparePartId);
    }

    /// <summary>Muestra el listado de piezas utilizadas en las reparaciones accesibles.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var repairParts = await VisibleRepairParts()
            .Include(repairPart => repairPart.Repair)
            .ThenInclude(repair => repair.Vehicle)
            .Include(repairPart => repairPart.SparePart)
            .OrderBy(repairPart => repairPart.RepairId)
            .ThenBy(repairPart => repairPart.Id)
            .ToListAsync();

        return View(repairParts);
    }

    /// <summary>Muestra los datos de una pieza usada tras validar acceso al recurso.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var repairPart = await context.RepairParts
            .Include(part => part.Repair)
            .ThenInclude(repair => repair.Vehicle)
            .Include(part => part.SparePart)
            .FirstOrDefaultAsync(part => part.Id == id);

        if (repairPart is null)
        {
            return NotFound();
        }

        var readAuth = await authorizationService.AuthorizeAsync(User, repairPart, WorkshopOperations.Read);
        if (!readAuth.Succeeded)
        {
            return Forbid();
        }

        ViewBag.CanEdit = (await authorizationService.AuthorizeAsync(User, repairPart, WorkshopOperations.Update)).Succeeded;
        ViewBag.CanDelete = (await authorizationService.AuthorizeAsync(User, repairPart, WorkshopOperations.Delete)).Succeeded;

        return View(repairPart);
    }

    /// <summary>Muestra el formulario unificado para añadir una pieza a una reparación.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Create(int? repairId = null)
    {
        if (repairId is > 0)
        {
            var repair = await context.Repairs.FindAsync(repairId.Value);
            if (repair is null)
            {
                return NotFound();
            }

            var auth = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.AddWork);
            if (!auth.Succeeded)
            {
                return Forbid();
            }
        }

        var model = new RepairPartFormViewModel
        {
            RepairId = repairId ?? 0,
            Quantity = 1
        };

        await PopulateDropdownsAsync(model);
        return View("Form", model);
    }

    /// <summary>Añade una pieza, valida permisos sobre la reparación y descuenta stock.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Create(RepairPartFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View("Form", model);
        }

        var repair = await context.Repairs
            .Include(item => item.RepairParts)
            .FirstOrDefaultAsync(item => item.Id == model.RepairId);
        if (repair is null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repair, WorkshopOperations.AddWork);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var sparePart = await context.SpareParts.FindAsync(model.SparePartId);
        if (sparePart is null)
        {
            return NotFound();
        }

        RepairPart addedRepairPart;
        try
        {
            addedRepairPart = repair.AddPart(sparePart, model.Quantity);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(RepairPartFormViewModel.Quantity), ex.Message);
            await PopulateDropdownsAsync(model);
            return View("Form", model);
        }

        await context.SaveChangesAsync();
        TempData["Message"] = "Pieza añadida correctamente a la reparación.";
        return RedirectToAction(nameof(Details), new { id = addedRepairPart.Id });
    }

    /// <summary>Muestra el formulario unificado para editar una pieza ya asignada.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Edit(int id)
    {
        var repairPart = await context.RepairParts
            .Include(part => part.Repair)
            .Include(part => part.SparePart)
            .FirstOrDefaultAsync(part => part.Id == id);
        if (repairPart is null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repairPart, WorkshopOperations.Update);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var model = new RepairPartFormViewModel
        {
            Id = repairPart.Id,
            RepairId = repairPart.RepairId,
            SparePartId = repairPart.SparePartId,
            Quantity = repairPart.Quantity,
            UnitPrice = repairPart.UnitPrice
        };

        await PopulateDropdownsAsync(model);
        return View("Form", model);
    }

    /// <summary>Actualiza una pieza y ajusta el stock según los cambios realizados.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Edit(int id, RepairPartFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        var repairPart = await context.RepairParts
            .Include(part => part.Repair)
            .Include(part => part.SparePart)
            .FirstOrDefaultAsync(part => part.Id == id);
        if (repairPart is null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repairPart, WorkshopOperations.Update);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var isAdmin = User.IsInRole(RoleNames.Admin);
        if (!isAdmin)
        {
            // Solo Admin puede modificar manualmente el precio histórico facturado.
            model.UnitPrice = repairPart.UnitPrice;
            ModelState.Remove(nameof(RepairPartFormViewModel.UnitPrice));
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View("Form", model);
        }

        var targetRepair = await context.Repairs.FindAsync(model.RepairId);
        var newSparePart = await context.SpareParts.FindAsync(model.SparePartId);
        if (targetRepair is null || newSparePart is null)
        {
            return NotFound();
        }

        if (targetRepair.Id != repairPart.RepairId)
        {
            var targetAuth = await authorizationService.AuthorizeAsync(User, targetRepair, WorkshopOperations.AddWork);
            if (!targetAuth.Succeeded)
            {
                return Forbid();
            }
        }

        var oldSparePart = repairPart.SparePart;
        var oldQuantity = repairPart.Quantity;
        var newQuantity = model.Quantity;
        var sameSparePart = oldSparePart.Id == newSparePart.Id;
        var availableStock = newSparePart.Stock;
        var stockAfterRestoringOldPart = sameSparePart ? availableStock + oldQuantity : availableStock;

        if (stockAfterRestoringOldPart < newQuantity)
        {
            ModelState.AddModelError(nameof(RepairPartFormViewModel.Quantity),
                "No hay stock suficiente para la cantidad seleccionada.");
            await PopulateDropdownsAsync(model);
            return View("Form", model);
        }

        if (!sameSparePart)
        {
            oldSparePart.Stock += oldQuantity;
        }

        newSparePart.Stock = stockAfterRestoringOldPart - newQuantity;
        repairPart.RepairId = targetRepair.Id;
        repairPart.Repair = targetRepair;
        repairPart.SparePartId = newSparePart.Id;
        repairPart.SparePart = newSparePart;
        repairPart.Quantity = newQuantity;
        if (isAdmin)
        {
            repairPart.UnitPrice = model.UnitPrice;
        }

        await context.SaveChangesAsync();
        TempData["Message"] = "Pieza de reparación actualizada correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Muestra la confirmación antes de eliminar una pieza de reparación.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Delete(int id)
    {
        var repairPart = await context.RepairParts
            .Include(part => part.Repair)
            .ThenInclude(repair => repair.Vehicle)
            .Include(part => part.SparePart)
            .FirstOrDefaultAsync(part => part.Id == id);

        if (repairPart is null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repairPart, WorkshopOperations.Delete);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        return View(repairPart);
    }

    /// <summary>Elimina la pieza y devuelve su cantidad al stock del recambio.</summary>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var repairPart = await context.RepairParts
            .Include(part => part.Repair)
            .Include(part => part.SparePart)
            .FirstOrDefaultAsync(part => part.Id == id);
        if (repairPart is null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, repairPart, WorkshopOperations.Delete);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        repairPart.SparePart.Stock += repairPart.Quantity;
        context.RepairParts.Remove(repairPart);
        await context.SaveChangesAsync();

        TempData["Message"] = "Pieza eliminada de la reparación y stock restablecido.";
        return RedirectToAction(nameof(Index));
    }
}
