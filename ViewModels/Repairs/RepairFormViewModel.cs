using System.ComponentModel.DataAnnotations;
using G3AspNet.Models.Enum;

namespace G3AspNet.ViewModels.Repairs;

/// <summary>DTO de formulario para alta y edición de reparaciones sin exponer colecciones ni relaciones.</summary>
public sealed class RepairFormViewModel
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Debes seleccionar un vehículo.")]
    [Display(Name = "Vehículo")]
    public int VehicleId { get; set; }

    [Display(Name = "Mecánico")]
    public string MechanicId { get; set; } = string.Empty;

    [Display(Name = "Tipo de reparación")]
    public RepairType RepairType { get; set; }

    [Display(Name = "Estado")]
    public RepairStatus Status { get; set; }

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string Description { get; set; } = string.Empty;

    [Display(Name = "Fecha de apertura")]
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Fecha de finalización")]
    public DateTime? CompletedAt { get; set; }

    [Range(0, 2000000, ErrorMessage = "El kilometraje debe estar entre 0 y 2.000.000.")]
    [Display(Name = "Kilometraje de entrada")]
    public int MileageAtEntry { get; set; }

    [Range(0, 1000, ErrorMessage = "Las horas de mano de obra deben estar entre 0 y 1000.")]
    [Display(Name = "Horas de mano de obra")]
    public decimal LaborHours { get; set; }

    [Range(0, 1000, ErrorMessage = "La tarifa por hora debe estar entre 0 y 1000.")]
    [Display(Name = "Tarifa por hora (€)")]
    public decimal HourlyRate { get; set; }
}
