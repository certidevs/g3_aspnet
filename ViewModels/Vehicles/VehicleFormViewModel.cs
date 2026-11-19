using System.ComponentModel.DataAnnotations;
using G3AspNet.Models.Enum;

namespace G3AspNet.ViewModels.Vehicles;

/// <summary>DTO de formulario para alta y edición de vehículos sin exponer propiedades de navegación.</summary>
public sealed class VehicleFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "La matrícula es obligatoria.")]
    [StringLength(10, ErrorMessage = "La matrícula no puede superar los 10 caracteres.")]
    [Display(Name = "Matrícula")]
    public string LicensePlate { get; set; } = string.Empty;

    [Required(ErrorMessage = "El VIN es obligatorio.")]
    [StringLength(17, ErrorMessage = "El VIN no puede superar los 17 caracteres.")]
    [Display(Name = "VIN")]
    public string VIN { get; set; } = string.Empty;

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [StringLength(50, ErrorMessage = "La marca no puede superar los 50 caracteres.")]
    [Display(Name = "Marca")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [StringLength(50, ErrorMessage = "El modelo no puede superar los 50 caracteres.")]
    [Display(Name = "Modelo")]
    public string Model { get; set; } = string.Empty;

    [Range(1950, 2100, ErrorMessage = "El año debe estar entre 1950 y 2100.")]
    [Display(Name = "Año")]
    public int Year { get; set; } = DateTime.Now.Year;

    [Display(Name = "Tipo de combustible")]
    public FuelType FuelType { get; set; }

    [Range(0, 2_000_000, ErrorMessage = "El kilometraje debe estar entre 0 y 2.000.000 km.")]
    [Display(Name = "Kilometraje")]
    public int Mileage { get; set; }

    [Required(ErrorMessage = "El propietario es obligatorio.")]
    [Display(Name = "Propietario")]
    public string OwnerId { get; set; } = string.Empty;
}
