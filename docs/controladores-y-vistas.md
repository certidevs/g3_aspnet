# Controladores y vistas

El proyecto sigue el patrón MVC: cada controlador agrupa acciones HTTP y sus vistas Razor homónimas se encuentran en `Views/<Controlador>/`.

Documentos relacionados: [Arquitectura](arquitectura.md) · [Autenticación y roles](autenticacion-y-roles.md) · [Guía funcional](guia-funcional.md).

## Controladores

| Controlador | Acciones principales | Función |
|---|---|---|
| `HomeController` | `Index`, `Error`, `ErrorStatus` | Portada pública con cifras del taller y páginas de error. |
| `AccountController` | `Login`, `LoginWith2fa`, `Register`, `Logout`, `AccessDenied` | Sesión, 2FA, registro y acceso denegado. |
| `ProfileController` | `Index`, `Edit`, `ChangePassword`, `TwoFactor`, `EnableTwoFactor`, `DisableTwoFactor` | Perfil propio y 2FA. |
| `UsersController` | `Index`, `Create`, `Details`, `Edit`, `Delete` | Administración de cuentas y roles. |
| `VehicleController` | `Index`, `Details`, `Create`, `Edit`, `Save`, `ConfirmDelete`, `Delete` | Gestión de vehículos. |
| `RepairsController` | `Index`, `Details`, `Create`, `Edit`, `Delete`, `Start`, `WaitForParts`, `Complete`, `Deliver`, `AddPart`, `RemovePart` | Reparaciones, cambios de estado y piezas desde el detalle. |
| `RepairPartsController` | `Index`, `Details`, `Create`, `Edit`, `Delete` | Líneas de piezas usadas. |
| `SparePartsController` | `Index`, `Details`, `Create`, `Edit`, `Save`, `Delete` | Catálogo de recambios. |

Las políticas y la autorización por recurso se detallan en [Autenticación y roles](autenticacion-y-roles.md). El módulo de reparaciones tiene su propia [documentación técnica](documentacion-tecnica-reparaciones.md).

## Vistas Razor

`Views/` contiene vistas agrupadas por controlador. `Views/_ViewStart.cshtml` asigna `_Layout`; `_ViewImports.cshtml` importa modelos, servicios y Tag Helpers. Los formularios de alta y edición del taller comparten una vista `Form.cshtml`.

`Views/Shared/` contiene:

| Vista | Uso |
|---|---|
| `_Layout.cshtml` | Navegación según el rol, usuario autenticado y mensajes de `TempData`. |
| `Error.cshtml`, `_ErrorPage.cshtml` | Páginas de error. |
| `_ClosedRepairConfirmation.cshtml` | Casilla de confirmación para modificar una reparación cerrada. |
| `_PasswordRequirements.cshtml` | Requisitos de contraseña en el registro. |
| `_ValidationScriptsPartial.cshtml` | Validación en el cliente. |

## Frontend

Los recursos se sirven desde `wwwroot/`:

- Bootstrap 5.3.3, jQuery 3.7.1, jQuery Validation 1.21.0 y Unobtrusive Validation 4.0.0, en `wwwroot/lib/`.
- Font Awesome 6.7.2, en `wwwroot/lib/font-awesome/`.
- Estilos propios en `wwwroot/css/site.css` y JavaScript en `wwwroot/js/site.js`.
- Imágenes de la aplicación e ilustraciones de error en `wwwroot/images/`; los avatares se guardan bajo `wwwroot/uploads/avatars/`.
- Las tipografías Oswald y Roboto se cargan desde Google Fonts.

El tema claro/oscuro usa el atributo de tema de Bootstrap y guarda la preferencia en el almacenamiento local del navegador.
