# Controladores y vistas

El proyecto sigue el patrón MVC: cada controlador agrupa acciones HTTP y sus vistas Razor homónimas se encuentran en `Views/<Controlador>/`.

Documentos relacionados: [Arquitectura](arquitectura.md) · [Autenticación y roles](autenticacion-y-roles.md) · [Guía funcional](guia-funcional.md).

## Controladores

| Controlador | Acciones principales | Función |
|---|---|---|
| `HomeController` | `Index`, `Error` | Inicio y página de error. |
| `AccountController` | `Login`, `Register`, `Logout`, `AccessDenied` | Sesión, registro y acceso denegado. |
| `ProfileController` | `Index`, `Edit`, `ChangePassword` | Consulta y edición del perfil propio. |
| `UsersController` | `Index`, `Create`, `Details`, `Edit`, `Delete` | Administración de cuentas y roles. |
| `VehicleController` | `Index`, `Details`, `Create`, `Edit`, `Save`, `Delete`, `ConfirmDelete` | Gestión de vehículos. |
| `RepairsController` | `Index`, `Details`, `Create`, `Edit`, `Delete`, `AddPart` | Reparaciones y piezas añadidas desde el detalle. |
| `RepairPartsController` | `Index`, `Details`, `Create`, `Edit`, `Delete` | Líneas de piezas usadas. |
| `SparePartsController` | `Index`, `Details`, `Create`, `Edit`, `Save`, `Delete` | Catálogo de recambios. |

Los atributos `[Authorize]` y sus diferencias por área se detallan en [Autenticación y roles](autenticacion-y-roles.md).

## Vistas Razor

`Views/` contiene vistas agrupadas por controlador. `Views/_ViewStart.cshtml` asigna `_Layout`; `_ViewImports.cshtml` importa modelos, servicios y Tag Helpers. `Views/Shared/` contiene el layout común, la vista de error y el partial de validación.

El layout muestra la navegación, el usuario autenticado y mensajes de `TempData`. Los formularios POST se protegen con el filtro global de antiforgery registrado en `Program.cs`.

## Frontend

Los recursos se sirven desde `wwwroot/`:

- Bootstrap 5.3.3 y jQuery 3.7.1, en `wwwroot/lib/`.
- Font Awesome 6.7.2, en `wwwroot/lib/font-awesome/`.
- Estilos propios en `wwwroot/css/site.css` y JavaScript en `wwwroot/js/site.js`.
- Imágenes de la aplicación en `wwwroot/images/`; los avatares se guardan bajo `wwwroot/uploads/avatars/`.

El tema claro/oscuro usa el atributo de tema de Bootstrap y guarda la preferencia en el almacenamiento local del navegador.
