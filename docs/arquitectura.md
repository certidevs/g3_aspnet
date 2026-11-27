# Arquitectura

TALLER .NET es una aplicación web ASP.NET Core MVC con vistas Razor. Los controladores reciben solicitudes, coordinan servicios y acceso a datos, y devuelven vistas o redirecciones.

Documentos relacionados: [Primeros pasos](primeros-pasos.md) · [Base de datos](base-de-datos-y-migraciones.md) · [Controladores y vistas](controladores-y-vistas.md).

## Estructura

| Carpeta | Responsabilidad |
|---|---|
| `Authorization/` | Nombres de políticas, operaciones del taller y handlers por recurso. |
| `Controllers/` | Acciones MVC y respuestas HTTP. |
| `Models/` | Entidades de Identity y del dominio, enums y nombres de roles. |
| `ViewModels/` | Datos de formularios y páginas, agrupados por funcionalidad. |
| `Services/` | Operaciones de usuario y almacenamiento/validación de avatares. |
| `Data/` | `ApplicationDbContext` e inicialización de la base y datos demo. |
| `Utilities/` | Datos del usuario autenticado, generación del QR de 2FA y mensaje de confirmación de reparaciones cerradas. |
| `Views/` | Vistas Razor organizadas por controlador y recursos compartidos. |
| `wwwroot/` | CSS, JavaScript, imágenes, librerías locales y avatares. |
| `tests/G3AspNet.Tests/` | Pruebas unitarias y de integración. |

## Solicitud HTTP y pipeline

```mermaid
flowchart LR
    Browser["Navegador"] --> Status["Páginas de estado 4xx/5xx"]
    Status --> Redirect["Redirección HTTPS"]
    Redirect --> Static{"Archivo estático?"}
    Static -->|Sí| Assets["wwwroot"]
    Static -->|No| Routing["Routing"]
    Routing --> Authentication["Autenticación"]
    Authentication --> Authorization["Autorización"]
    Authorization --> Limiter["Límite de peticiones"]
    Limiter --> Controller["Controlador (ruta MVC convencional)"]
    Controller --> Resource["Autorización por recurso"]
    Resource --> Services["Servicios / EF Core"]
    Services --> SQLite["SQLite"]
    Controller --> Razor["Vista Razor"]
    Razor --> Response["Respuesta HTTP"]
    Assets --> Response
```

- Fuera de `Development`, el pipeline añade el manejador de excepciones (`/Home/Error`) y HSTS.
- `UseStatusCodePagesWithReExecute` completa las respuestas 4xx y 5xx sin contenido con `/Home/ErrorStatus/{código}`.
- Los formularios POST se protegen mediante un filtro global de antiforgery.
- La política por defecto (`FallbackPolicy`) exige sesión iniciada. Las páginas públicas se marcan con `[AllowAnonymous]`.

Los controladores se enrutan con el patrón convencional:

```text
{controller=Home}/{action=Index}/{id?}
```

## Inyección de dependencias

- `ApplicationDbContext` se registra como scoped y usa SQLite.
- ASP.NET Core Identity usa `ApplicationDbContext` para persistir usuarios y roles.
- `VehicleAuthorizationHandler`, `RepairAuthorizationHandler` y `RepairPartAuthorizationHandler` son scoped.
- `UserService` es scoped.
- `ImageStorage` es singleton y recibe opciones desde la sección `FileUploads:Images`.
- `ImageStorageOptions`, `FormOptions`, el limitador de peticiones y las políticas `GestionTaller` y `SoloAdmin` se configuran al iniciar la aplicación.

La autorización se explica en [Autenticación y roles](autenticacion-y-roles.md).

El detalle de entidades, cardinalidades y borrado está en [Modelo de datos](diagrama-er.md); la estructura de `Program.cs` y la política de datos están en [Base de datos y migraciones](base-de-datos-y-migraciones.md).
