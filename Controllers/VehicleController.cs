using G3AspNet.Data;
using G3AspNet.Models;
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
    // Roles del personal del taller: los únicos que pueden crear, editar y borrar vehículos.
    // Si en tu proyecto los permisos son otros, cambia solo esta línea.
    private const string StaffRoles = RoleNames.Admin + "," + RoleNames.Mechanic;

    private readonly ApplicationDbContext context;
    private readonly UserManager<ApplicationUser> userManager;

    // ASP.NET Core proporciona el contexto y el UserManager al crear el controlador.
    // userManager se usa para saber quién es el usuario actual y para listar los clientes.
    public VehicleController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        this.context = context;
        this.userManager = userManager;
    }

    /// <summary>
    /// Vehículos que puede ver el usuario actual: un Customer solo ve los suyos;
    /// el resto de roles (Admin, Mechanic...) ven todos.
    /// Devuelve la consulta SIN ejecutar, para poder seguir añadiendo Include y OrderBy.
    /// </summary>
    private IQueryable<Vehicle> VisibleVehicles()
    {
        var vehicles = context.Vehicles.AsQueryable();

        if (User.IsInRole(RoleNames.Customer))
        {
            var currentUserId = userManager.GetUserId(User);
            vehicles = vehicles.Where(vehicle => vehicle.OwnerId == currentUserId);
        }

        return vehicles;
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

    /// <summary>Muestra un vehículo con su propietario y su historial de reparaciones.</summary>
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        // Se filtra también aquí: si no, un Customer podría escribir el Id de un vehículo
        // ajeno en la URL y verlo. Si no es suyo, la consulta no lo encuentra y devuelve 404.
        var vehicle = await VisibleVehicles()
            .Include(vehicle => vehicle.Owner)
            .Include(vehicle => vehicle.Repairs)
            .ThenInclude(repair => repair.Mechanic)
            .FirstOrDefaultAsync(vehicle => vehicle.Id == id);

        if (vehicle == null)
        {
            return NotFound();
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
    [Authorize(Roles = StaffRoles)]
    public async Task<IActionResult> Create()
    {
        await PopulateOwnersAsync();

        // Un vehículo nuevo tiene Id = 0: así sabe Save que tiene que INSERTAR.
        return View("Form", new Vehicle { Year = DateTime.Now.Year });
    }

    /// <summary>Muestra el formulario relleno con los datos de un vehículo existente.</summary>
    [HttpGet]
    [Authorize(Roles = StaffRoles)]
    public async Task<IActionResult> Edit(int id)
    {
        var vehicle = await context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        await PopulateOwnersAsync(vehicle.OwnerId);
        return View("Form", vehicle);
    }

    /// <summary>Guarda el formulario: crea el vehículo si Id es 0 y lo actualiza si no.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = StaffRoles)]
    public async Task<IActionResult> Save(Vehicle vehicle)
    {
        // El formulario solo envía OwnerId, no el objeto Owner completo: lo quitamos de la validación.
        ModelState.Remove(nameof(Vehicle.Owner));

        // La matrícula es única en la base de datos: se comprueba antes de guardar para no dar un error 500.
        var plateInUse = await context.Vehicles
            .AnyAsync(other => other.LicensePlate == vehicle.LicensePlate && other.Id != vehicle.Id);
        if (plateInUse)
        {
            ModelState.AddModelError(nameof(Vehicle.LicensePlate), "Ya existe un vehículo con esa matrícula.");
        }

        // El propietario tiene que ser una cuenta que exista (clave foránea). Si llega vacío, ya lo avisa [Required].
        var ownerExists = await context.Users.AnyAsync(user => user.Id == vehicle.OwnerId);
        if (!ownerExists && !string.IsNullOrEmpty(vehicle.OwnerId))
        {
            ModelState.AddModelError(nameof(Vehicle.OwnerId), "El propietario seleccionado no existe.");
        }

        // Si algo falla, se vuelve al formulario con los datos escritos y la lista de clientes.
        if (!ModelState.IsValid)
        {
            await PopulateOwnersAsync(vehicle.OwnerId);
            return View("Form", vehicle);
        }

        if (vehicle.Id == 0)
        {
            context.Vehicles.Add(vehicle);
        }
        else
        {
            // Edición: buscamos el vehículo guardado y le copiamos los campos del formulario.
            var existing = await context.Vehicles.FindAsync(vehicle.Id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.LicensePlate = vehicle.LicensePlate;
            existing.VIN = vehicle.VIN;
            existing.Brand = vehicle.Brand;
            existing.Model = vehicle.Model;
            existing.Year = vehicle.Year;
            existing.FuelType = vehicle.FuelType;
            existing.Mileage = vehicle.Mileage;
            existing.OwnerId = vehicle.OwnerId;
        }

        await context.SaveChangesAsync();

        TempData["Message"] = "Vehículo guardado correctamente.";
        return RedirectToAction(nameof(Details), new { id = vehicle.Id });
    }

    /// <summary>Elimina un vehículo, salvo que tenga reparaciones en su historial.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = StaffRoles)]
    public async Task<IActionResult> Delete(int id)
    {
        var vehicle = await context.Vehicles.FindAsync(id);
        if (vehicle == null)
        {
            return NotFound();
        }

        // La relación Repair -> Vehicle es Restrict (las reparaciones son historial): la base de datos
        // no deja borrar un vehículo con reparaciones. Lo comprobamos antes y avisamos con un mensaje.
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
}
