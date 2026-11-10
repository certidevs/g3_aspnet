using G3AspNet.Data;
using G3AspNet.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

[Authorize]
public class RepairPartsController : Controller
{
    private readonly ApplicationDbContext context;

    /// <summary>Inicializa el controlador con el contexto de la base de datos.</summary>
    /// <param name="context">Contexto usado para consultar y guardar reparaciones y recambios.</param>
    public RepairPartsController(ApplicationDbContext context)
    {
        this.context = context;
    }

    /// <summary>Prepara las listas de reparaciones y recambios para los formularios.</summary>
    /// <param name="repairPart">Pieza cuyos valores deben quedar seleccionados, si corresponde.</param>
    private async Task PopulateDropdownsAsync(RepairPart? repairPart = null)
    {
        var repairs = await context.Repairs
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

    /// <summary>Muestra el listado de piezas utilizadas en las reparaciones.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var repairParts = await context.RepairParts
            .Include(repairPart => repairPart.Repair)
            .ThenInclude(repair => repair.Vehicle)
            .Include(repairPart => repairPart.SparePart)
            .OrderBy(repairPart => repairPart.RepairId)
            .ThenBy(repairPart => repairPart.Id)
            .ToListAsync();

        return View(repairParts);
    }

    /// <summary>Muestra los datos de una pieza usada o devuelve 404 si no existe.</summary>
    /// <param name="id">Identificador de la pieza de reparación.</param>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var repairPart = await context.RepairParts
            .Include(part => part.Repair)
            .ThenInclude(repair => repair.Vehicle)
            .Include(part => part.SparePart)
            .FirstOrDefaultAsync(part => part.Id == id);

        return repairPart is null ? NotFound() : View(repairPart);
    }

    // TODO (issue #18): Create y Edit usan hoy dos vistas casi iguales (Create.cshtml y Edit.cshtml).
    // Mejora pendiente: unirlas en un único Views/RepairParts/Form.cshtml y devolver View("Form", ...).

    /// <summary>Muestra el formulario para añadir una pieza a una reparación.</summary>
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await PopulateDropdownsAsync();
        return View();
    }

    /// <summary>Añade una pieza, valida los datos y descuenta las unidades del stock.</summary>
    /// <param name="repairPart">Datos de reparación, recambio, cantidad y precio.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RepairPart repairPart)
    {
        ModelState.Remove(nameof(RepairPart.Repair));
        ModelState.Remove(nameof(RepairPart.SparePart));
        ValidateRepairPart(repairPart);

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(repairPart);
            return View(repairPart);
        }

        var repair = await context.Repairs
            .Include(item => item.RepairParts)
            .FirstOrDefaultAsync(item => item.Id == repairPart.RepairId);
        if (repair is null)
        {
            return NotFound();
        }

        var sparePart = await context.SpareParts.FindAsync(repairPart.SparePartId);
        if (sparePart is null)
        {
            return NotFound();
        }

        RepairPart addedRepairPart;
        try
        {
            addedRepairPart = repair.AddPart(sparePart, repairPart.Quantity);
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(RepairPart.Quantity), ex.Message);
            await PopulateDropdownsAsync(repairPart);
            return View(repairPart);
        }

        await context.SaveChangesAsync();
        TempData["Message"] = "Pieza añadida correctamente a la reparación.";
        return RedirectToAction(nameof(Details), new { id = addedRepairPart.Id });
    }

    /// <summary>Muestra el formulario para editar una pieza ya asignada.</summary>
    /// <param name="id">Identificador de la pieza de reparación.</param>
    [HttpGet]
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

        await PopulateDropdownsAsync(repairPart);
        return View(repairPart);
    }

    /// <summary>Actualiza una pieza y ajusta el stock según los cambios realizados.</summary>
    /// <param name="id">Identificador de la pieza que se va a editar.</param>
    /// <param name="model">Nuevos datos de la pieza de reparación.</param>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RepairPart model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        ModelState.Remove(nameof(RepairPart.Repair));
        ModelState.Remove(nameof(RepairPart.SparePart));
        ValidateRepairPart(model);

        if (!ModelState.IsValid)
        {
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        var repairPart = await context.RepairParts
            .Include(part => part.SparePart)
            .FirstOrDefaultAsync(part => part.Id == id);
        if (repairPart is null)
        {
            return NotFound();
        }

        var repair = await context.Repairs.FindAsync(model.RepairId);
        var newSparePart = await context.SpareParts.FindAsync(model.SparePartId);
        if (repair is null || newSparePart is null)
        {
            return NotFound();
        }

        var oldSparePart = repairPart.SparePart;
        var oldQuantity = repairPart.Quantity;
        var newQuantity = model.Quantity;
        var sameSparePart = oldSparePart.Id == newSparePart.Id;
        var availableStock = newSparePart.Stock;
        var stockAfterRestoringOldPart = sameSparePart ? availableStock + oldQuantity : availableStock;

        if (stockAfterRestoringOldPart < newQuantity)
        {
            ModelState.AddModelError(nameof(RepairPart.Quantity),
                "No hay stock suficiente para la cantidad seleccionada.");
            await PopulateDropdownsAsync(model);
            return View(model);
        }

        if (!sameSparePart)
        {
            oldSparePart.Stock += oldQuantity;
        }

        newSparePart.Stock = stockAfterRestoringOldPart - newQuantity;
        repairPart.RepairId = repair.Id;
        repairPart.Repair = repair;
        repairPart.SparePartId = newSparePart.Id;
        repairPart.SparePart = newSparePart;
        repairPart.Quantity = newQuantity;
        repairPart.UnitPrice = model.UnitPrice;

        await context.SaveChangesAsync();
        TempData["Message"] = "Pieza de reparación actualizada correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    /// <summary>Muestra la confirmación antes de eliminar una pieza de reparación.</summary>
    /// <param name="id">Identificador de la pieza de reparación.</param>
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var repairPart = await context.RepairParts
            .Include(part => part.Repair)
            .ThenInclude(repair => repair.Vehicle)
            .Include(part => part.SparePart)
            .FirstOrDefaultAsync(part => part.Id == id);

        return repairPart is null ? NotFound() : View(repairPart);
    }

    /// <summary>Elimina la pieza y devuelve su cantidad al stock del recambio.</summary>
    /// <param name="id">Identificador de la pieza que se va a eliminar.</param>
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var repairPart = await context.RepairParts
            .Include(part => part.SparePart)
            .FirstOrDefaultAsync(part => part.Id == id);
        if (repairPart is null)
        {
            return NotFound();
        }

        repairPart.SparePart.Stock += repairPart.Quantity;
        context.RepairParts.Remove(repairPart);
        await context.SaveChangesAsync();

        TempData["Message"] = "Pieza eliminada de la reparación y stock restablecido.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Valida las referencias y los límites de cantidad y precio de una pieza.</summary>
    /// <param name="repairPart">Pieza cuyos datos se validan.</param>
    private void ValidateRepairPart(RepairPart repairPart)
    {
        if (repairPart.RepairId <= 0)
        {
            ModelState.AddModelError(nameof(RepairPart.RepairId), "Debes seleccionar una reparación.");
        }

        if (repairPart.SparePartId <= 0)
        {
            ModelState.AddModelError(nameof(RepairPart.SparePartId), "Debes seleccionar un recambio.");
        }

        if (repairPart.Quantity is < 1 or > 1000)
        {
            ModelState.AddModelError(nameof(RepairPart.Quantity), "La cantidad debe estar entre 1 y 1000.");
        }

        if (repairPart.UnitPrice is < 0 or > 100000)
        {
            ModelState.AddModelError(nameof(RepairPart.UnitPrice), "El precio debe estar entre 0 y 100000.");
        }
    }
}
