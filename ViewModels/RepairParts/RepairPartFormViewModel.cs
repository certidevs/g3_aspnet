using System.ComponentModel.DataAnnotations;

namespace G3AspNet.ViewModels.RepairParts;

/// <summary>DTO de formulario para alta y edición de piezas usadas en una reparación.</summary>
public sealed class RepairPartFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar una reparación.")]
    [Display(Name = "Reparación")]
    public int RepairId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un recambio.")]
    [Display(Name = "Recambio")]
    public int SparePartId { get; set; }

    [Range(1, 1000, ErrorMessage = "La cantidad debe estar entre 1 y 1000.")]
    [Display(Name = "Cantidad")]
    public int Quantity { get; set; } = 1;

    [Range(0, 100000, ErrorMessage = "El precio debe estar entre 0 y 100000.")]
    [Display(Name = "Precio unitario (€)")]
    public decimal UnitPrice { get; set; }
}
