using System.ComponentModel.DataAnnotations.Schema;
using G3AspNet.Models.Enum;

namespace G3AspNet.Models;

public class Repair
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public string MechanicId { get; set; } = string.Empty;
    public RepairType RepairType { get; set; }
    public RepairStatus Status { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int MileageAtEntry { get; set; }
    public decimal LaborHours { get; set; }
    public decimal HourLyRate { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public ApplicationUser Mechanic { get; set; } = null!;

    // Piezas asociadas: deben estar cargadas para que el total las incluya.
    public ICollection<RepairPart> RepairParts { get; set; } = new List<RepairPart>();

    // Mano de obra (horas × tarifa) más piezas (cantidad × precio).
    // Se calcula al consultar la propiedad y no se almacena en la base de datos.
    [NotMapped]
    public decimal TotalCost => LaborHours * HourLyRate
                                + RepairParts.Sum(part => part.Quantity * part.UnitPrice);
}
