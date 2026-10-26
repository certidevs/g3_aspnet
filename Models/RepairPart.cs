using G3AspNet.Models.Enum;

namespace G3AspNet.Models;

public class RepairPart
{
    /*Id (int, PK)
       RepairId (int, FK → Repair)
       SparePartId (int, FK → SparePart)
       Quantity (int)
       UnitPrice (decimal): precio en el momento de usarla (el del recambio puede cambiar luego)*/
    
    public int Id { get; set; }
    public int RepairId { get; set; }
    public int SparePartId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
