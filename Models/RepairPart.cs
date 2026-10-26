using G3AspNet.Models.Enum;

namespace G3AspNet.Models;

public class RepairPart
{
    public int Id { get; set; }
    public int RepairId { get; set; }
    public int SparePartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
