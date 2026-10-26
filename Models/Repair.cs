using G3AspNet.Models.Enum;

namespace G3AspNet.Models;

public class Repair
{
    public int Id { get; set; }
    public int VehicleId { get; set; }
    public int MechanicId { get; set; }
    public RepairType RepairType { get; set; }
    public RepairStatus Status { get; set; }
    public DateTime OpenedAt { get; set; }
    public DateTime CompletedAt { get; set; }
    public int MileageAtEntry { get; set; }
    public decimal LaborHours { get; set; }
    public decimal HourLyRate { get; set; }
}
