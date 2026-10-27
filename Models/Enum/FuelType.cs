using System.ComponentModel.DataAnnotations;

namespace G3AspNet.Models.Enum;

public enum FuelType
{
    [Display(Name = "Gasolina")]
    Petrol,

    [Display(Name = "Diésel")]
    Diesel,

    [Display(Name = "Híbrido")]
    Hybrid,

    [Display(Name = "Eléctrico")]
    Electric,

    [Display(Name = "GLP")]
    LPG
}