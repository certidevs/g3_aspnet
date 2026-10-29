using G3AspNet.Data;
using Microsoft.AspNetCore.Mvc;

namespace G3AspNet.Controllers;

public class SparePartsController : Controller
{
    private readonly ApplicationDbContext context;

    public SparePartsController(ApplicationDbContext context)
    {
        this.context = context;
    }

    public IActionResult Index()
    {
        var spareParts = context.SpareParts.ToList();
        return View(spareParts);
    }
}
