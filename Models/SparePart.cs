using System.ComponentModel.DataAnnotations;

namespace G3AspNet.Models;

/// <summary>Recambio del almacén del taller: lo que se puede montar en una reparación.</summary>
public class SparePart
{
    public int Id { get; set; }

    /// <summary>Referencia del fabricante. Es única: no puede haber dos recambios con la misma.</summary>
    [Required]
    [StringLength(50)]
    public string Reference { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string Brand { get; set; } = string.Empty;

    /// <summary>Precio de venta actual. Cada línea de reparación guarda el suyo (RepairPart.UnitPrice).</summary>
    [Range(0, 100000)]
    public decimal UnitPrice { get; set; }

    /// <summary>Unidades disponibles en el almacén. Baja al usar el recambio en una reparación.</summary>
    [Range(0, 100000)]
    public int Stock { get; set; }

    /// <summary>Stock mínimo: por debajo (o igual) hay que volver a pedir el recambio.</summary>
    [Range(0, 100000)]
    public int MinStock { get; set; }

    // Líneas de reparación en las que se ha usado este recambio (colección inversa).
    public ICollection<RepairPart> RepairParts { get; set; } = new List<RepairPart>();
}
