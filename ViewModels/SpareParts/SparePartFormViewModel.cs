using System.ComponentModel.DataAnnotations;

namespace G3AspNet.ViewModels.SpareParts;

/// <summary>DTO de formulario para alta y edición de recambios del almacén.</summary>
public sealed class SparePartFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "La referencia es obligatoria.")]
    [StringLength(50, ErrorMessage = "La referencia no puede superar los 50 caracteres.")]
    [Display(Name = "Referencia")]
    public string Reference { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre")]
    public string Name { get; set; } = string.Empty;

    [StringLength(50, ErrorMessage = "La marca no puede superar los 50 caracteres.")]
    [Display(Name = "Marca")]
    public string Brand { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "El precio debe estar entre 0 y 100000.")]
    [Display(Name = "Precio unitario (€)")]
    public decimal UnitPrice { get; set; }

    [Range(0, 100000, ErrorMessage = "El stock debe estar entre 0 y 100000.")]
    [Display(Name = "Stock (unidades)")]
    public int Stock { get; set; }

    [Range(0, 100000, ErrorMessage = "El stock mínimo debe estar entre 0 y 100000.")]
    [Display(Name = "Stock mínimo")]
    public int MinStock { get; set; }
}
