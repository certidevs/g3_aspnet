namespace G3AspNet.Utilities;

/// <summary>Confirmación que se exige antes de modificar una reparación completada o entregada.</summary>
public static class ClosedRepairConfirmation
{
    public const string Required =
        "La reparación está cerrada. Marca la casilla de confirmación para modificarla.";
}
