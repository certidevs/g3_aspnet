namespace G3AspNet.ViewModels.Errors;

/// <summary>Textos e ilustración de una página de error.</summary>
public sealed record ErrorPageViewModel(
    int Code,
    string Tag,
    string Title,
    string Highlight,
    string Message,
    string Image,
    string? RequestId = null)
{
    /// <summary>Devuelve el contenido que corresponde a un código de estado HTTP.</summary>
    public static ErrorPageViewModel ForStatus(int code) => code switch
    {
        403 => new(code, "Error 403 · Acceso denegado", "Solo personal", "autorizado",
            "No tienes permiso para acceder a esta página. Vuelve al inicio o entra con una cuenta autorizada.",
            "error-403.svg"),
        404 => new(code, "Error 404 · Página no encontrada", "Te has salido", "de la ruta",
            "La página o el registro que buscas no existe o ha cambiado de sitio.",
            "error-404.svg"),
        429 => new(code, "Error 429 · Demasiadas peticiones", "Frena un", "momento",
            "Has hecho demasiados intentos seguidos. Espera un minuto antes de volver a probar.",
            "error-429.svg"),
        >= 500 => new(code, $"Error {code} · Fallo interno", "Avería en", "el taller",
            "Algo ha fallado y no hemos podido completar la petición. Inténtalo de nuevo en unos minutos.",
            "error-500.svg"),
        400 => new(code, "Error 400 · Petición no válida", "Algo no", "encaja",
            "No hemos podido procesar la petición. Si enviabas un formulario, vuelve atrás, recarga la página e inténtalo de nuevo.",
            "error-generico.svg"),
        _ => new(code, $"Error {code}", "Algo no", "encaja",
            "No hemos podido completar la petición. Vuelve al inicio e inténtalo de nuevo.",
            "error-generico.svg")
    };
}
