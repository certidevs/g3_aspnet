using G3AspNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

[Authorize]
/// <summary>Gestiona la consulta de reparaciones y las piezas empleadas en cada intervención.</summary>
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
