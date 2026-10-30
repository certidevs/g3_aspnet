using G3AspNet.Data;
using G3AspNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

[Authorize]
/// <summary>Gestiona la consulta de reparaciones y las piezas empleadas en cada intervención.</summary>
public class RepairController : Controller
{
    private readonly ApplicationDbContext context;
    private readonly UserManager<ApplicationUser> userManager;

    public RepairController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        this.context = context;
        this.userManager = userManager;
    }

    private async Task PopulateDropdownsAsync(Repair? repair = null)
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

    /// <summary>Lista todas las reparaciones incluyendo su vehículo y mecánico asignado.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var repairs = await context.Repairs
            .Include(repair => repair.Vehicle)
            .Include(repair => repair.Mechanic)
            .ToListAsync();

        return View(repairs);
    }

    /// <summary>Muestra el detalle de una reparación junto con las piezas y recambios utilizados.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        // Se cargan las relaciones necesarias para mostrar los datos y calcular TotalCost.
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

        return View(repair);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();

        var madridTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        var madridNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, madridTz);
        var defaultTime = new DateTime(madridNow.Year, madridNow.Month, madridNow.Day, madridNow.Hour, madridNow.Minute, 0);

        return View(new Repair
        {
            OpenedAt = defaultTime,
            CompletedAt = defaultTime
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Repair repair)
    {
        ModelState.Remove(nameof(Repair.Vehicle));
        ModelState.Remove(nameof(Repair.Mechanic));

        if (repair.VehicleId <= 0)
        {
            ModelState.AddModelError(nameof(Repair.VehicleId), "Debes seleccionar un vehículo.");
        }

        if (string.IsNullOrWhiteSpace(repair.MechanicId))
        {
            ModelState.AddModelError(nameof(Repair.MechanicId), "Debes seleccionar un mecánico.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(repair);
            return View(repair);
        }

        context.Repairs.Add(repair);
        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación creada correctamente.";
        return RedirectToAction(nameof(Details), new { id = repair.Id });
    }

    [HttpGet]
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

        await PopulateDropdownsAsync(repair);
        return View(repair);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Repair model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        ModelState.Remove(nameof(Repair.Vehicle));
        ModelState.Remove(nameof(Repair.Mechanic));

        if (model.VehicleId <= 0)
        {
            ModelState.AddModelError(nameof(Repair.VehicleId), "Debes seleccionar un vehículo.");
        }

        if (string.IsNullOrWhiteSpace(model.MechanicId))
        {
            ModelState.AddModelError(nameof(Repair.MechanicId), "Debes seleccionar un mecánico.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var repair = await context.Repairs.FindAsync(id);
        if (repair == null)
        {
            return NotFound();
        }

        repair.VehicleId = model.VehicleId;
        repair.MechanicId = model.MechanicId;
        repair.RepairType = model.RepairType;
        repair.Status = model.Status;
        repair.OpenedAt = model.OpenedAt;
        repair.CompletedAt = model.CompletedAt;
        repair.MileageAtEntry = model.MileageAtEntry;
        repair.LaborHours = model.LaborHours;
        repair.HourLyRate = model.HourLyRate;

        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación actualizada correctamente.";
        return RedirectToAction(nameof(Details), new { id = repair.Id });
    }

    [HttpGet]
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

        return View(repair);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var repair = await context.Repairs
            .Include(r => r.RepairParts)
            .ThenInclude(rp => rp.SparePart)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (repair == null)
        {
            return NotFound();
        }

        foreach (var part in repair.RepairParts)
        {
            if (part.SparePart != null)
            {
                part.SparePart.Stock = (part.SparePart.Stock ?? 0) + part.Quantity;
            }
        }

        context.Repairs.Remove(repair);
        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación eliminada correctamente.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Añade un recambio a la reparación y descuenta la cantidad del stock disponible.</summary>
    [HttpPost]
    public async Task<IActionResult> AddPart(int id, int sparePartId, int quantity)
    {
        // Cargamos la reparación con sus piezas actuales.
        var repair = await context.Repairs
            .Include(r => r.RepairParts)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (repair == null)
        {
            return NotFound();
        }

        // Buscamos el recambio que se quiere añadir para comprobar y actualizar su stock.
        var sparePart = await context.SpareParts.FindAsync(sparePartId);
        if (sparePart == null)
        {
            return NotFound();
        }

        try
        {
            // La entidad valida la cantidad y el stock, descuenta las unidades y crea el RepairPart.
            repair.AddPart(sparePart, quantity);
            await context.SaveChangesAsync();
            TempData["Message"] = "Pieza añadida correctamente.";
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
        {
            // Si la cantidad no es válida o no hay stock suficiente, informamos del motivo en la vista.
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Details), new { id });
    }
}
