using G3AspNet.Authorization;
using G3AspNet.Data;
using G3AspNet.Models;
using G3AspNet.Utilities;
using G3AspNet.ViewModels.Vehicles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

/// <summary>Gestión de los vehículos del taller y consulta de su historial de reparaciones.</summary>
[Authorize]
public class VehicleController : Controller
{
    private readonly ApplicationDbContext context;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly IAuthorizationService authorizationService;

    public VehicleController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        IAuthorizationService authorizationService)
    {
        this.context = context;
        this.userManager = userManager;
        this.authorizationService = authorizationService;
    }

    /// <summary>
    /// Vehículos visibles en el listado: Admin y Mechanic ven todos;
    /// Customer y User solo ven los vehículos de su propiedad.
    /// </summary>
    private IQueryable<Vehicle> VisibleVehicles()
    {
        var vehicles = context.Vehicles.AsQueryable();

        if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Mechanic))
        {
            return vehicles;
        }

        var currentUserId = User.GetRequiredUserId();
        return vehicles.Where(vehicle => vehicle.OwnerId == currentUserId);
    }

    /// <summary>Lista los vehículos visibles para el usuario, incluyendo su propietario.</summary>
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var vehicles = await VisibleVehicles()
            .Include(vehicle => vehicle.Owner)
            .OrderBy(vehicle => vehicle.LicensePlate)
            .ToListAsync();

        return View(vehicles);
    }

    /// <summary>Muestra un vehículo con su propietario y su historial de reparaciones tras autorizar el recurso.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var vehicle = await context.Vehicles
            .Include(vehicle => vehicle.Owner)
            .Include(vehicle => vehicle.Repairs)
            .ThenInclude(repair => repair.Mechanic)
            .FirstOrDefaultAsync(vehicle => vehicle.Id == id);

        if (vehicle == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, vehicle, WorkshopOperations.Read);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        return View(vehicle);
    }

    /// <summary>Prepara la lista de clientes (rol Customer) para el desplegable de propietario.</summary>
    private async Task PopulateOwnersAsync(string? selectedOwnerId = null)
    {
        var customers = await userManager.GetUsersInRoleAsync(RoleNames.Customer);
        ViewBag.Owners = new SelectList(
            customers
                .OrderBy(customer => customer.DisplayName ?? customer.UserName)
                .Select(customer => new { customer.Id, Name = customer.DisplayName ?? customer.UserName }),
            "Id", "Name", selectedOwnerId);
    }

    /// <summary>Muestra el formulario vacío para dar de alta un vehículo.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Create()
    {
        await PopulateOwnersAsync();
        return View("Form", new VehicleFormViewModel { Year = DateTime.Now.Year });
    }

    /// <summary>Muestra el formulario relleno con los datos de un vehículo existente.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Edit(int id)
    {
        var vehicle = await context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, vehicle, WorkshopOperations.Update);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        await PopulateOwnersAsync(vehicle.OwnerId);
        return View("Form", new VehicleFormViewModel
        {
            Id = vehicle.Id,
            LicensePlate = vehicle.LicensePlate,
            VIN = vehicle.VIN,
            Brand = vehicle.Brand,
            Model = vehicle.Model,
            Year = vehicle.Year,
            FuelType = vehicle.FuelType,
            Mileage = vehicle.Mileage,
            OwnerId = vehicle.OwnerId
        });
    }

    /// <summary>Guarda el formulario mediante DTO anti-overposting: crea o actualiza el vehículo.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Save(VehicleFormViewModel formModel)
    {
        // La matrícula se compara ya recortada y sin distinguir mayúsculas (el índice único de la base de datos
        // sí las distingue, y los espacios sobrantes se recortan al guardar): así "1234abc" y " 1234ABC" cuentan como repetidas.
        var licensePlate = formModel.LicensePlate.Trim();
        var plateInUse = await context.Vehicles
            .AnyAsync(other => other.LicensePlate.ToUpper() == licensePlate.ToUpper() && other.Id != formModel.Id);
        if (plateInUse)
        {
            ModelState.AddModelError(nameof(VehicleFormViewModel.LicensePlate), "Ya existe un vehículo con esa matrícula.");
        }

        var ownerExists = await context.Users.AnyAsync(user => user.Id == formModel.OwnerId);
        if (!ownerExists && !string.IsNullOrEmpty(formModel.OwnerId))
        {
            ModelState.AddModelError(nameof(VehicleFormViewModel.OwnerId), "El propietario seleccionado no existe.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateOwnersAsync(formModel.OwnerId);
            return View("Form", formModel);
        }

        int savedId;
        if (formModel.Id == 0)
        {
            var entity = new Vehicle
            {
                LicensePlate = licensePlate,
                VIN = formModel.VIN.Trim(),
                Brand = formModel.Brand.Trim(),
                Model = formModel.Model.Trim(),
                Year = formModel.Year,
                FuelType = formModel.FuelType,
                Mileage = formModel.Mileage,
                OwnerId = formModel.OwnerId
            };
            context.Vehicles.Add(entity);
            await context.SaveChangesAsync();
            savedId = entity.Id;
        }
        else
        {
            var existing = await context.Vehicles.FindAsync(formModel.Id);
            if (existing == null)
            {
                return NotFound();
            }

            var authResult = await authorizationService.AuthorizeAsync(User, existing, WorkshopOperations.Update);
            if (!authResult.Succeeded)
            {
                return Forbid();
            }

            existing.LicensePlate = licensePlate;
            existing.VIN = formModel.VIN.Trim();
            existing.Brand = formModel.Brand.Trim();
            existing.Model = formModel.Model.Trim();
            existing.Year = formModel.Year;
            existing.FuelType = formModel.FuelType;
            existing.Mileage = formModel.Mileage;
            existing.OwnerId = formModel.OwnerId;

            await context.SaveChangesAsync();
            savedId = existing.Id;
        }

        TempData["Message"] = "Vehículo guardado correctamente.";
        return RedirectToAction(nameof(Details), new { id = savedId });
    }

    /// <summary>Elimina un vehículo, salvo que tenga reparaciones en su historial.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> Delete(int id)
    {
        var vehicle = await context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, vehicle, WorkshopOperations.Delete);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        var hasRepairs = await context.Repairs.AnyAsync(repair => repair.VehicleId == id);
        if (hasRepairs)
        {
            TempData["Error"] = "No se puede eliminar el vehículo: tiene reparaciones en su historial.";
            return RedirectToAction(nameof(Details), new { id });
        }

        context.Vehicles.Remove(vehicle);
        await context.SaveChangesAsync();

        TempData["Message"] = "Vehículo eliminado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Muestra la página de confirmación antes de eliminar un vehículo.</summary>
    [HttpGet]
    [Authorize(Policy = PolicyNames.GestionTaller)]
    public async Task<IActionResult> ConfirmDelete(int id)
    {
        var vehicle = await context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        var authResult = await authorizationService.AuthorizeAsync(User, vehicle, WorkshopOperations.Delete);
        if (!authResult.Succeeded)
        {
            return Forbid();
        }

        return View("Delete", vehicle);
    }
}
