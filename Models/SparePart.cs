using Microsoft.AspNetCore.Identity;

namespace G3AspNet.Models;

public class SparePart
{
    public int Id {get; set;}
    public string Name {get; set;} = string.Empty;
    public string Brand {get; set;} = string.Empty;
    public decimal? UnitPrice {get; set;}
    public int? Stock {get; set;}
    public int? MinStock {get; set;}

//Relacion con RepairPart

    public int RepairPartID {get; set;}
    public RepairPart? repairPart {get; set;}

}