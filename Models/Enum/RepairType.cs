using System.ComponentModel.DataAnnotations;

namespace G3AspNet.Models.Enum;

public enum RepairType
{
    [Display(Name = "Cambio de aceite")]
    OilChange,
    [Display(Name = "Inspección general")]
    GeneralInspection,
    [Display(Name = "Cambio de líquido de frenos")]
    BrakeFluidChange,
    [Display(Name = "Neumáticos")]
    Tires,
    [Display(Name = "Carrocería")]
    Bodywork,
    [Display(Name = "Pintura")]
    Painting,
    [Display(Name = "Otro")]
    Other
}
