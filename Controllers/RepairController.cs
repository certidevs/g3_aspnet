using G3AspNet.Data;
using G3AspNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

[Authorize]
/// <summary>Gestiona la consulta, creación, edición y eliminación de reparaciones y las piezas empleadas en cada intervención.</summary>
public class RepairController : Controller
{
    private readonly ApplicationDbContext context;
    private readonly UserManager<ApplicationUser> userManager;

    public RepairController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        this.context = context;
        this.userManager = userManager;
    }

    /// <summary>Rellena los desplegables de vehículos y mecánicos para los formularios.</summary>
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

    /// <summary>Muestra el detalle de una reparación, sus recambios y el selector de piezas disponibles.</summary>
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

        // Carga de recambios con stock para el formulario de adición en la vista usando UnitPrice
        ViewBag.AvailableSpareParts = new SelectList(
            await context.SpareParts
                .AsNoTracking()
                .Where(sp => sp.Stock > 0)
                .OrderBy(sp => sp.Name)
                .Select(sp => new { sp.Id, Description = $"{sp.Name} (Stock: {sp.Stock} - {sp.UnitPrice:C})" })
                .ToListAsync(),
            "Id",
            "Description");

        return View(repair);
    }

    /// <summary>Muestra el formulario unificado (Form.cshtml) para dar de alta una nueva reparación.</summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();

        var madridTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
        var madridNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, madridTz);
        var defaultTime = new DateTime(madridNow.Year, madridNow.Month, madridNow.Day, madridNow.Hour, madridNow.Minute, 0);

        var model = new Repair
        {
            OpenedAt = defaultTime,
            CompletedAt = defaultTime
        };

        return View("Form", model);
    }

    /// <summary>Registra una nueva reparación en la base de datos.</summary>
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
            return View("Form", repair);
        }

        context.Repairs.Add(repair);
        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación creada correctamente.";
        return RedirectToAction(nameof(Details), new { id = repair.Id });
    }

    /// <summary>Muestra el formulario unificado (Form.cshtml) precargado para editar una reparación.</summary>
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
        return View("Form", repair);
    }

    /// <summary>Actualiza los datos de una reparación existente.</summary>
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
            return View("Form", model);
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

    /// <summary>Muestra la vista de confirmación previa antes de eliminar una reparación.</summary>
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

    /// <summary>Elimina la reparación y devuelve al inventario el stock de las piezas asociadas.</summary>
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

        // Devolver al stock las unidades de cada pieza empleada antes de borrar la reparación
        foreach (var part in repair.RepairParts)
        {
            if (part.SparePart != null)
            {
                part.SparePart.Stock = (part.SparePart.Stock ?? 0) + part.Quantity;
            }
        }

        context.Repairs.Remove(repair);
        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación eliminada correctamente y stock devuelto.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Añade un recambio a la reparación y descuenta la cantidad del stock disponible.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPart(int id, int sparePartId, int quantity)
    {
        var repair = await context.Repairs
            .Include(r => r.RepairParts)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (repair == null)
        {
            return NotFound();
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
}