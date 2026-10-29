using G3AspNet.Data;
using G3AspNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

[Authorize]
/// <summary>Gestiona la consulta, creación, edición y eliminación de reparaciones y las piezas empleadas en cada intervención.</summary>
public class RepairController : Controller
{
    private readonly ApplicationDbContext context;

    // ASP.NET Core proporciona el contexto al crear el controlador.
    public RepairController(ApplicationDbContext context)
    {
        this.context = context;
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

    /// <summary>Muestra el formulario para dar de alta una nueva reparación.</summary>
    [HttpGet]
    public IActionResult Create()
    {
        return View(new Repair
        {
            OpenedAt = DateTime.UtcNow
        });
    }

    /// <summary>Registra una nueva reparación en la base de datos.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Repair repair)
    {
        // Ignoramos propiedades de navegación en la validación del modelo recibido por formulario.
        ModelState.Remove(nameof(Repair.Vehicle));
        ModelState.Remove(nameof(Repair.Mechanic));

        if (!ModelState.IsValid)
        {
            return View(repair);
        }

        context.Repairs.Add(repair);
        await context.SaveChangesAsync();

        TempData["Message"] = "Reparación creada correctamente.";
        return RedirectToAction(nameof(Details), new { id = repair.Id });
    }

    /// <summary>Muestra el formulario para editar una reparación existente.</summary>
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

        return View(repair);
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

        if (!ModelState.IsValid)
        {
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

    /// <summary>Muestra la confirmación para eliminar una reparación.</summary>
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

        // Devolver al stock las unidades de cada pieza empleada antes de borrar la reparación.
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

