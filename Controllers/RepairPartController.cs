using G3AspNet.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

[Authorize]
/// <summary>Gestiona las líneas de recambios asociados a una reparación y su impacto en el stock.</summary>
public class RepairPartController : Controller
{
    private readonly ApplicationDbContext context;

    // ASP.NET Core proporciona el contexto al crear el controlador.
    public RepairPartController(ApplicationDbContext context)
    {
        this.context = context;
    }

    /// <summary>Lista las piezas utilizadas, opcionalmente filtradas por reparación.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(int? repairId)
    {
        var query = context.RepairParts
            .Include(rp => rp.Repair)
            .Include(rp => rp.SparePart)
            .AsQueryable();

        if (repairId.HasValue)
        {
            query = query.Where(rp => rp.RepairId == repairId.Value);
        }

        return View(await query.ToListAsync());
    }

    /// <summary>Añade un recambio a una reparación descontando el stock disponible.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int repairId, int sparePartId, int quantity, decimal? unitPrice = null)
    {
        var repair = await context.Repairs
            .Include(r => r.RepairParts)
            .FirstOrDefaultAsync(r => r.Id == repairId);

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
            repair.AddPart(sparePart, quantity, unitPrice);
            await context.SaveChangesAsync();
            TempData["Message"] = "Pieza añadida correctamente.";
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException or InvalidOperationException)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Details", "Repair", new { id = repairId });
    }

    /// <summary>Actualiza la cantidad de una pieza en la reparación ajustando el stock del recambio.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, int quantity, decimal? unitPrice = null)
    {
        var repairPart = await context.RepairParts
            .Include(rp => rp.SparePart)
            .FirstOrDefaultAsync(rp => rp.Id == id);

        if (repairPart == null)
        {
            return NotFound();
        }

        if (quantity <= 0)
        {
            TempData["Error"] = "La cantidad debe ser mayor que cero.";
            return RedirectToAction("Details", "Repair", new { id = repairPart.RepairId });
        }

        int diff = quantity - repairPart.Quantity;
        int availableStock = repairPart.SparePart.Stock ?? 0;

        if (diff > 0 && availableStock < diff)
        {
            TempData["Error"] = "No hay stock suficiente para aumentar la cantidad.";
            return RedirectToAction("Details", "Repair", new { id = repairPart.RepairId });
        }

        repairPart.SparePart.Stock = availableStock - diff;
        repairPart.Quantity = quantity;
        if (unitPrice.HasValue)
        {
            repairPart.UnitPrice = unitPrice.Value;
        }

        await context.SaveChangesAsync();
        TempData["Message"] = "Línea de pieza actualizada correctamente.";

        return RedirectToAction("Details", "Repair", new { id = repairPart.RepairId });
    }

    /// <summary>Elimina una pieza de la reparación y devuelve las unidades al stock.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var repairPart = await context.RepairParts
            .Include(rp => rp.SparePart)
            .FirstOrDefaultAsync(rp => rp.Id == id);

        if (repairPart == null)
        {
            return NotFound();
        }

        int repairId = repairPart.RepairId;

        // Devolver las unidades al stock del recambio
        repairPart.SparePart.Stock = (repairPart.SparePart.Stock ?? 0) + repairPart.Quantity;

        context.RepairParts.Remove(repairPart);
        await context.SaveChangesAsync();

        TempData["Message"] = "Pieza eliminada de la reparación y stock actualizado.";
        return RedirectToAction("Details", "Repair", new { id = repairId });
    }
}

