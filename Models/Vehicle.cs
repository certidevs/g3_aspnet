using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using G3AspNet.Models.Enum;

namespace G3AspNet.Models;

public class Vehicle
{
    [Key]
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
    [Display(Name = "Marca")]
    public string Brand { get; set; } = string.Empty;

    [Required(ErrorMessage = "El modelo es obligatorio.")]
    [Display(Name = "Modelo")]
    public string Model { get; set; } = string.Empty;

    [Range(1950, 2100, ErrorMessage = "El año debe estar entre 1950 y 2100.")]
    [Display(Name = "Año")]
    public int Year { get; set; }

    [Display(Name = "Tipo de combustible")]
    public FuelType FuelType { get; set; }

    [Range(0, 2_000_000, ErrorMessage = "El kilometraje debe estar entre 0 y 2.000.000 km.")]
    [Display(Name = "Kilometraje")]
    public int Mileage { get; set; }

    [Required(ErrorMessage = "El propietario es obligatorio.")]
    [Display(Name = "Propietario")]
    public string OwnerId { get; set; } = string.Empty;

    [ForeignKey(nameof(OwnerId))]
    public ApplicationUser Owner { get; set; } = null!;

    public List<Repair> Repairs { get; set; } = new();
}