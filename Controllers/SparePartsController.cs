using G3AspNet.Data;
using G3AspNet.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

/// <summary>
/// Almacén de recambios del taller. Es el EJEMPLO COMPLETO del proyecto: listado, detalle, alta,
/// edición y borrado funcionando de principio a fin. Copiad su estructura en vuestras entidades.
/// </summary>
public class SparePartsController : Controller
{
    private readonly ApplicationDbContext context;

    public SparePartsController(ApplicationDbContext context)
    {
        this.context = context;
    }

    /// <summary>Lista todos los recambios ordenados por nombre.</summary>
    [HttpGet]
    public async Task<IActionResult> Index(bool lowStock = false)
    {

        var query = context.SpareParts.AsQueryable();

        if (lowStock)
        {
            query = query.Where(sparePart => sparePart.Stock <= sparePart.MinStock);
        }

        var spareParts = await context.SpareParts
            .OrderBy(sparePart => sparePart.Name)
            .ToListAsync();

        ViewBag.LowStockFilter = lowStock; 

        return View(spareParts);
    }

    /// <summary>Muestra un recambio y las reparaciones en las que se ha usado.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var sparePart = await context.SpareParts
            .Include(sparePart => sparePart.RepairParts)
            .ThenInclude(repairPart => repairPart.Repair)
            .ThenInclude(repair => repair.Vehicle)
            .FirstOrDefaultAsync(sparePart => sparePart.Id == id);

        if (sparePart == null)
        {
            return NotFound();
        }

        return View(sparePart);
    }

    // TODO (issue #19): marcar el stock bajo (Stock <= MinStock) en el listado y en el detalle.

    // ---------------------------------------------------------------------------------
    // EJEMPLO COMPLETO (issues #19 y #20): Create, Edit, Save y Delete con una única
    // vista Views/SpareParts/Form.cshtml. Es el modelo que hay que copiar en el resto.
    // ---------------------------------------------------------------------------------

    /// <summary>Muestra el formulario vacío para dar de alta un recambio nuevo.</summary>
    [HttpGet]
    public IActionResult Create()
    {
        // Se reutiliza la vista Form.cshtml, que sirve para crear y para editar.
        // Un recambio nuevo tiene Id = 0: así sabe Save que tiene que INSERTAR.
        return View("Form", new SparePart());
    }

    /// <summary>Muestra el formulario relleno con los datos de un recambio existente.</summary>
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var sparePart = await context.SpareParts.FindAsync(id);
        if (sparePart == null)
        {
            return NotFound();
        }

        // Mismo formulario que Create, pero con los datos del recambio. Su Id es mayor que 0,
        // así que Save lo ACTUALIZARÁ en lugar de crear otro.
        return View("Form", sparePart);
    }

    /// <summary>Guarda el formulario: crea el recambio si Id es 0 y lo actualiza si no.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SparePart sparePart)
    {
        // 1) Las validaciones de los atributos del modelo ([Required], [Range]...) ya se han aplicado
        //    antes de entrar aquí: el resultado está en ModelState.
        // 2) Regla propia: la referencia es única en la base de datos. La comprobamos nosotros
        //    antes de guardar; si no, SQLite lanzaría una excepción y el usuario vería un error 500.
        var referenceInUse = await context.SpareParts
            .AnyAsync(other => other.Reference == sparePart.Reference && other.Id != sparePart.Id);
        if (referenceInUse)
        {
            ModelState.AddModelError(nameof(SparePart.Reference), "Ya existe un recambio con esa referencia.");
        }

        // 3) Si algo falla, se vuelve al formulario con lo que escribió el usuario y los mensajes de error.
        if (!ModelState.IsValid)
        {
            return View("Form", sparePart);
        }

        if (sparePart.Id == 0)
        {
            // Alta: EF Core genera el Id al guardar.
            context.SpareParts.Add(sparePart);
        }
        else
        {
            // Edición: buscamos el recambio guardado y le copiamos los campos del formulario.
            var existing = await context.SpareParts.FindAsync(sparePart.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Reference = sparePart.Reference;
            existing.Name = sparePart.Name;
            existing.Brand = sparePart.Brand;
            existing.UnitPrice = sparePart.UnitPrice;
            existing.Stock = sparePart.Stock;
            existing.MinStock = sparePart.MinStock;
        }

        await context.SaveChangesAsync();

        // TempData["Message"] se muestra una sola vez en el _Layout, después de redirigir.
        TempData["Message"] = "Recambio guardado correctamente.";
        return RedirectToAction(nameof(Details), new { id = sparePart.Id });
    }

    /// <summary>Elimina un recambio, salvo que ya se haya usado en alguna reparación.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var sparePart = await context.SpareParts.FindAsync(id);
        if (sparePart == null)
        {
            return NotFound();
        }

        // La relación RepairPart -> SparePart es Restrict: la base de datos no deja borrar un recambio
        // que ya está en alguna reparación. Lo comprobamos antes y avisamos con un mensaje,
        // para que la excepción no llegue al usuario como un error 500.
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
