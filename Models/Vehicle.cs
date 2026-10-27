using System.ComponentModel.DataAnnotations;
using G3AspNet.Models.Enum;

namespace G3AspNet.Models;

public class Vehicle
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "La matrícula es obligatoria.")]
    [Display(Name = "Matrícula")]
    public string LicensePlate { get; set; } = string.Empty;

    [Required(ErrorMessage = "El VIN es obligatorio.")]
    [Display(Name = "VIN")]
    public string VIN { get; set; } = string.Empty;

    [Required(ErrorMessage = "La marca es obligatoria.")]
    [Display(Name = "Marca")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [Display(Name = "Modelo")]
    public string Model { get; set; } = string.Empty;

    [Required(ErrorMessage = "El año es obligatorio.")]
    [Display(Name = "Año")]
    public int Year { get; set; }

    [Required(ErrorMessage = "El tipo de combustible es obligatorio.")]
    [Display(Name = "Tipo de combustible")]
    public FuelType FuelType { get; set; }

    [Required(ErrorMessage = "El kilometraje es obligatorio.")]
    [Display(Name = "Kilometraje")]
    public int Mileage { get; set; }

    [Required(ErrorMessage = "El propietario es obligatorio.")]
    [Display(Name = "Propietario")]
    public string OwnerId { get; set; } = string.Empty;
}