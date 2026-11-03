using System.ComponentModel.DataAnnotations;


namespace G3AspNet.Models;

public class RepairPart
{
    public int Id { get; set; }
    public int RepairId { get; set; }
    public int SparePartId { get; set; }
    [Range(1, 1000)] 
    public int Quantity { get; set; }
    [Range(0, 100000)] 
    public decimal UnitPrice { get; set; }

    // Navegaciones asociadas a RepairId y SparePartId; se deben cargar para usarlas.
    public Repair Repair { get; set; } = null!;
    public SparePart SparePart { get; set; } = null!;
}
