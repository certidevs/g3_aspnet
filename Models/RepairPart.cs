using G3AspNet.Models.Enum;

namespace G3AspNet.Models;

public class RepairPart
{
    public int Id { get; set; }
    public int RepairId { get; set; }
    public int SparePartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Navegaciones asociadas a RepairId y SparePartId; se deben cargar para usarlas.
    public Repair Repair { get; set; } = null!;
    public SparePart SparePart { get; set; } = null!;
}
