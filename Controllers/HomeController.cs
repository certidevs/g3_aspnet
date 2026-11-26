using G3AspNet.Data;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
using G3AspNet.ViewModels.Errors;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace G3AspNet.Controllers;

/// <summary>Página de inicio pública y vista de error.</summary>
[AllowAnonymous]
public sealed class HomeController(ApplicationDbContext context) : Controller
{
    public async Task<IActionResult> Index()
    {
        // Cifras agregadas: no exponen datos personales, por eso la portada puede ser pública.
        ViewData["Vehicles"] = await context.Vehicles.CountAsync();
        ViewData["Repairs"] = await context.Repairs.CountAsync(r => r.Status == RepairStatus.Completed || r.Status == RepairStatus.Delivered);
        ViewData["InProgress"] = await context.Repairs.CountAsync(r => r.Status == RepairStatus.InProgress);
        ViewData["SpareParts"] = await context.SpareParts.CountAsync();
        return View();
    }

    // La petición fallida puede ser un POST sin token válido: la página de error no lo exige.
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });

    /// <summary>Página para las respuestas de error sin contenido; conserva el código de estado.</summary>
    [IgnoreAntiforgeryToken]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult ErrorStatus(int id)
    {
        var code = id is >= 400 and <= 599 ? id : StatusCodes.Status404NotFound;
        Response.StatusCode = code;
        return View(ErrorPageViewModel.ForStatus(code));
    }
}
