using System.ComponentModel.DataAnnotations;
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
    [Required]
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }

    [Range(0, 2000000)]
    public int MileageAtEntry { get; set; }

    [Range(0, 1000)]
    public decimal LaborHours { get; set; }

    [Range(0, 1000)]
    public decimal HourlyRate { get; set; }

    [ForeignKey(nameof(VehicleId))]
    public Vehicle Vehicle { get; set; } = null!;
    [ForeignKey(nameof(MechanicId))]
    public ApplicationUser Mechanic { get; set; } = null!;

    // Piezas asociadas: deben estar cargadas para que el total las incluya.
    public ICollection<RepairPart> RepairParts { get; set; } = new List<RepairPart>();

    // Mano de obra (horas × tarifa) más piezas (cantidad × precio).
    // Se calcula al consultar la propiedad y no se almacena en la base de datos.
    [NotMapped]
    public decimal TotalCost => LaborHours * HourlyRate
                                + RepairParts.Sum(part => part.Quantity * part.UnitPrice);

    /// <summary>
    /// Añade una pieza a la reparación y descuenta la cantidad del stock del recambio.
    /// </summary>
    public RepairPart AddPart(SparePart sparePart, int quantity, decimal? unitPrice = null)
    {
        ArgumentNullException.ThrowIfNull(sparePart);

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), "La cantidad debe ser mayor que cero.");
        }

        if (sparePart.Stock < quantity)
        {
            throw new InvalidOperationException("No hay stock suficiente para el recambio seleccionado.");
        }

        sparePart.Stock -= quantity;

        var repairPart = new RepairPart
        {
            Repair = this,
            RepairId = Id,
            SparePart = sparePart,
            SparePartId = sparePart.Id,
            Quantity = quantity,
            UnitPrice = unitPrice ?? sparePart.UnitPrice
        };

        RepairParts.Add(repairPart);
        return repairPart;
    }
}
