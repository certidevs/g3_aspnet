using G3AspNet.Data;
using G3AspNet.Models;
using G3AspNet.Models.Enum;
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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
    });
}
