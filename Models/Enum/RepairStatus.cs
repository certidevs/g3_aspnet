using System.ComponentModel.DataAnnotations;

namespace G3AspNet.Models.Enum;

public enum RepairStatus
{
    [Display(Name = "Pendiente")]
    Pending,
    [Display(Name = "En progreso")] 
    InProgress,
    [Display(Name = "Esperando repuestos")]
    WaitingParts,
    [Display(Name = "Completado")] 
    Completed,
    [Display(Name = "Entregado")] 
    Delivered
}
