using G3AspNet.Authorization;
using G3AspNet.Data;
using G3AspNet.Models;
using G3AspNet.Utilities;
using G3AspNet.ViewModels.SpareParts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

/// <summary>Almacén de recambios del taller con control de acceso por políticas.</summary>
[Authorize]
public class SparePartsController : Controller
{
    private readonly ApplicationDbContext context;

    public SparePartsController(ApplicationDbContext context)
    {
        this.context = context;
    }

    /// <summary>Lista todos los recambios ordenados por nombre (opcionalmente filtrando stock bajo).</summary>
    [HttpGet]
    public async Task<IActionResult> Index(bool lowStock = false)
    {
        var query = context.SpareParts.AsQueryable();

        if (lowStock)
        {
            query = query.Where(sparePart => sparePart.Stock <= sparePart.MinStock);
        }

        var spareParts = await query
            .OrderBy(sparePart => sparePart.Name)
            .ToListAsync();

        ViewBag.LowStockFilter = lowStock;

        return View(spareParts);
    }

    /// <summary>Muestra un recambio y las reparaciones accesibles en las que se ha usado.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var sparePart = await context.SpareParts
            .AsNoTracking()
            .Include(sp => sp.RepairParts)
            .ThenInclude(repairPart => repairPart.Repair)
            .ThenInclude(repair => repair.Vehicle)
            .FirstOrDefaultAsync(sp => sp.Id == id);

        if (sparePart == null)
        {
            return NotFound();
        }

        if (!User.IsInRole(RoleNames.Admin))
        {
            var userId = User.GetRequiredUserId();
            sparePart.RepairParts = User.IsInRole(RoleNames.Mechanic)
                ? sparePart.RepairParts.Where(rp => rp.Repair.MechanicId == userId).ToList()
                : sparePart.RepairParts.Where(rp => rp.Repair.Vehicle.OwnerId == userId).ToList();
        }

        return View(sparePart);
    }

    /// <summary>Muestra el formulario vacío para dar de alta un recambio nuevo.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public IActionResult Create()
    {
        return View("Form", new SparePartFormViewModel());
    }

    /// <summary>Muestra el formulario relleno con los datos de un recambio existente.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Edit(int id)
    {
        var sparePart = await context.SpareParts.FindAsync(id);
        if (sparePart == null)
        {
            return NotFound();
        }

        var model = new SparePartFormViewModel
        {
            Id = sparePart.Id,
            Reference = sparePart.Reference,
            Name = sparePart.Name,
            Brand = sparePart.Brand,
            UnitPrice = sparePart.UnitPrice,
            Stock = sparePart.Stock,
            MinStock = sparePart.MinStock
        };

        return View("Form", model);
    }

    /// <summary>Guarda el formulario usando DTO anti-overposting: crea si Id es 0 y actualiza si no.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Save(SparePartFormViewModel model)
    {
        var normalizedReference = model.Reference.Trim();
        var referenceInUse = await context.SpareParts
            .AnyAsync(other => other.Reference == normalizedReference && other.Id != model.Id);
        if (referenceInUse)
        {
            ModelState.AddModelError(nameof(SparePartFormViewModel.Reference), "Ya existe un recambio con esa referencia.");
        }

        if (!ModelState.IsValid)
        {
            return View("Form", model);
        }

        int savedId;
        if (model.Id == 0)
        {
            var entity = new SparePart
            {
                Reference = normalizedReference,
                Name = model.Name.Trim(),
                Brand = model.Brand.Trim(),
                UnitPrice = model.UnitPrice,
                Stock = model.Stock,
                MinStock = model.MinStock
            };
            context.SpareParts.Add(entity);
            await context.SaveChangesAsync();
            savedId = entity.Id;
        }
        else
        {
            var existing = await context.SpareParts.FindAsync(model.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Reference = normalizedReference;
            existing.Name = model.Name.Trim();
            existing.Brand = model.Brand.Trim();
            existing.UnitPrice = model.UnitPrice;
            existing.Stock = model.Stock;
            existing.MinStock = model.MinStock;
            await context.SaveChangesAsync();
            savedId = existing.Id;
        }

        TempData["Message"] = "Recambio guardado correctamente.";
        return RedirectToAction(nameof(Details), new { id = savedId });
    }

    /// <summary>Elimina un recambio, salvo que ya se haya usado en alguna reparación.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Delete(int id)
    {
        var sparePart = await context.SpareParts.FindAsync(id);
        if (sparePart == null)
        {
            return NotFound();
        }

        var isUsed = await context.RepairParts.AnyAsync(repairPart => repairPart.SparePartId == id);
        if (isUsed)
        {
            TempData["Error"] = "No se puede eliminar el recambio: se ha usado en alguna reparación.";
            return RedirectToAction(nameof(Details), new { id });
        }

        context.SpareParts.Remove(sparePart);
        await context.SaveChangesAsync();

        TempData["Message"] = "Recambio eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
