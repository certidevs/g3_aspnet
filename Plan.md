
# GRUPO 3 - TALLER .NET

> Estado revisado el 26/11/2026 sobre la rama `main` (commit `b61dc49`). El proyecto de pruebas queda fuera de este planning.
> Las funcionalidades del apartado 4 y el reparto del apartado 6 son una **propuesta** pendiente de acordar en el equipo.

## 1. Resumen

### De qué va la aplicación

Aplicación web para gestionar un taller mecánico: clientes y sus vehículos, reparaciones desde que se abren hasta que se entregan, almacén de recambios y un área privada donde cada cliente consulta lo suyo.

Stack: .NET 10, ASP.NET Core MVC con Razor, Entity Framework Core con SQLite, ASP.NET Core Identity y Bootstrap.

### Roles de usuario y qué puede hacer cada uno

Sin iniciar sesión solo se accede a la portada, el login y el registro.

| Rol | Qué puede hacer hoy |
|---|---|
| `Admin` | Gestiona cuentas y roles. Gestiona vehículos, recambios y todas las reparaciones. Es el único que asigna mecánico y tarifa, entrega vehículos, elimina reparaciones y cambia el precio de una pieza usada. |
| `Mechanic` | Gestiona vehículos y recambios. Ve y trabaja solo sus reparaciones asignadas: crea, edita, cambia de estado y añade o quita piezas mientras no estén cerradas. |
| `Customer` | Consulta sus vehículos, sus reparaciones, las piezas usadas en ellas y el catálogo de recambios. No modifica nada del taller. |
| `User` | Rol que recibe quien se registra. Gestiona su perfil y puede consultar el catálogo. No tiene vehículos hasta que un `Admin` le cambia el rol a `Customer`. |

Todos los roles pueden editar su perfil y avatar, cambiar la contraseña, activar 2FA y cambiar el tema.

### Integrantes

- Iván López ([irilopa](https://github.com/irilopa))
- Ricardo Horcajo ([ricardohsm1996](https://github.com/ricardohsm1996))
- Rubén García ([rubogar](https://github.com/rubogar))

## 2. Qué tenemos hecho

La aplicación compila sin errores ni avisos y arranca con datos de ejemplo.

**Cuentas y seguridad**

- Registro, login con usuario o email, logout y página de acceso denegado.
- Bloqueo tras 5 intentos fallidos y límite de peticiones en login y registro.
- Verificación en dos pasos (TOTP).
- Perfil con avatar (validado por extensión, tamaño y contenido) y cambio de contraseña.
- Administración de usuarios: búsqueda, alta, edición, rol, activar/desactivar y baja. Protege la cuenta propia y al último administrador.
- Autorización por políticas y por recurso: cada cliente ve solo lo suyo y cada mecánico solo sus reparaciones.

**Taller**

- CRUD de vehículos con propietario, matrícula única e historial de reparaciones.
- CRUD de reparaciones con tipo, descripción, kilometraje, horas y tarifa.
- Flujo de estados: Pendiente → En progreso ⇄ Esperando repuestos; En progreso → Completada → Entregada. Para completar hay que haber apuntado horas.
- CRUD de recambios con referencia única, stock y stock mínimo, y aviso visual de stock bajo.
- Piezas por reparación, desde el detalle o desde «Piezas usadas», con ajuste automático de stock y precio histórico.
- Coste total calculado (mano de obra + piezas).
- Protección del historial: no se borran vehículos con reparaciones ni recambios usados.

**Interfaz y entrega**

- Portada pública con cifras del taller, colores corporativos y tema claro u oscuro.
- Datos de ejemplo, Dockerfile, integración continua, documentación y wiki.

## 3. Errores y cosas a medias

### Errores

No hay errores al compilar ni al arrancar. E1 a E7 se han reproducido ejecutando una copia de la aplicación; E8 a E10 salen de la lectura del código y la documentación.

| # | Prioridad | Error | Dónde |
|---|---|---|---|
| E1 | Alta | Eliminar un usuario con vehículos o reparaciones asignadas da error 500 (`DbUpdateException`). Debe mostrar un mensaje y ofrecer desactivar la cuenta. | `Services/UserService.cs` (`DeleteAsync`) |
| E2 | Alta | Eliminar una reparación completada o entregada devuelve al stock piezas que ya se montaron. | `Controllers/RepairsController.cs` (`DeleteConfirmed`) |
| E3 | Media | El formulario de reparación muestra la fecha de apertura en UTC y el detalle en hora local. Una hora escrita a mano se guarda desplazada. | `RepairsController`, `Views/Repairs/Form.cshtml` |
| E4 | Media | Se puede asignar una reparación a una cuenta que no es mecánico (por ejemplo, un cliente) manipulando la petición. | `RepairsController` (`Create`, `Edit`) |
| E5 | Media | Editar una reparación con un mecánico inexistente (petición manipulada) da error 500. | `RepairsController` (`Edit`) |
| E6 | Media | Un vehículo admite un VIN de un solo carácter y un propietario que no es cliente (esto último, manipulando la petición). | `VehicleController` (`Save`), `VehicleFormViewModel` |
| E7 | Baja | Algunos mensajes de validación y de Identity salen en inglés. | Registro, usuarios, perfil |
| E8 | Media | Un `Admin` puede editar horas y tarifa de una reparación completada o entregada, y cambia el coste ya cerrado. Confirmar si es intencionado. | `Authorization/RepairAuthorizationHandler.cs` |
| E9 | Media | La imagen Docker usa `Production` pero carga las cuentas de demostración, porque `SeedData:Enabled` es `true` por defecto. | `appsettings.json`, `Dockerfile` |
| E10 | Baja | La documentación no coincide con el código: dice puerto `10000` (el Dockerfile usa `8080`), bloqueo de 10 minutos (son 15) y una matriz de permisos antigua. | `README.md`, `docs/` |

### Cosas a medias

- **Filtro de stock bajo:** `SparePartsController.Index` admite `lowStock=true`, pero ninguna vista ofrece el botón.
- **Entrega:** el estado «Entregado» no guarda la fecha de entrega.
- **Kilometraje:** abrir una reparación no comprueba ni actualiza el kilometraje del vehículo.
- **Portada:** promete presupuesto y que el cliente registre una reparación con prioridad; ninguna de las dos cosas existe.
- **Registro:** muestra un desplegable de rol con una única opción.
- **2FA:** se configura copiando la clave, sin código QR ni códigos de recuperación.
- **Nombres:** conviven «TALLER .NET» y «G3 ASP.NET», y «Esperando repuestos» con «Esperando recambios».

## 4. Qué falta de lógica de negocio para MVP

### 4.1 Solicitud de cita

- **Qué hace:** como `Customer` quiero pedir cita para uno de mis vehículos para no tener que llamar al taller.
- **Reglas:**
  - Solo para vehículos propios.
  - Descripción obligatoria y fecha deseada posterior a hoy.
  - Se impide una segunda solicitud pendiente del mismo vehículo.
  - El cliente puede cancelar mientras esté pendiente.
  - Solo `Admin` acepta o rechaza. Al rechazar hay que indicar el motivo.
  - Al aceptar se elige mecánico y se crea una reparación «Pendiente».
- **Modelo:** nueva entidad `Appointment` (`Id`, `VehicleId`, `RequestedDate`, `RepairType`, `Description`, `Status`, `RejectionReason?`, `RepairId?`, `CreatedAt`) y enum `AppointmentStatus` (Pendiente, Aceptada, Rechazada, Cancelada). `DbSet`, relaciones con `Restrict` y datos de ejemplo.
- **Controlador y acciones:** `AppointmentsController`: `Index`, `Create` (GET/POST), `Cancel`, `Accept`, `Reject`.
- **Vistas:** `Appointments/Index`, `Appointments/Form`, botón «Pedir cita» en el detalle del vehículo y entrada en el menú Taller.
- **Responsable:** Iván.

### 4.2 Presupuesto y aprobación del cliente

- **Qué hace:** como `Customer` quiero aprobar o rechazar el presupuesto para saber cuánto voy a pagar antes de que empiece la reparación.
- **Reglas:**
  - El mecánico asignado o `Admin` envía el importe estimado con la reparación «Pendiente».
  - Solo el propietario del vehículo aprueba o rechaza.
  - No se puede iniciar una reparación sin presupuesto aprobado.
  - Un presupuesto aprobado no se modifica.
  - El detalle compara el importe estimado con el coste real.
- **Modelo:** en `Repair`, `EstimatedCost?`, `BudgetStatus` (Sin enviar, Pendiente, Aprobado, Rechazado) y `BudgetAnsweredAt?`. Actualizar los datos de ejemplo.
- **Controlador y acciones:** `RepairsController`: `SendBudget`, `ApproveBudget`, `RejectBudget` y comprobación en `Start`. Nueva operación en `WorkshopOperations` y en `RepairAuthorizationHandler`.
- **Vistas:** `Repairs/Details` (importe, estado y botones según rol) y columna de presupuesto en `Repairs/Index`.
- **Responsable:** Rafa.

### 4.3 Reposición de stock

- **Qué hace:** como `Mechanic` quiero registrar las entradas de recambios para saber qué hay y qué falta por pedir.
- **Reglas:**
  - Solo `Admin` y `Mechanic`.
  - Cantidad mayor que cero; la entrada suma al stock.
  - Cada entrada guarda fecha, usuario y coste de compra.
  - El stock deja de editarse a mano en un recambio ya creado.
  - El listado permite filtrar los recambios con stock igual o inferior al mínimo.
- **Modelo:** nueva entidad `StockEntry` (`Id`, `SparePartId`, `Quantity`, `UnitCost`, `CreatedAt`, `UserId`, `Notes?`). `DbSet`, relaciones y datos de ejemplo.
- **Controlador y acciones:** `SparePartsController`: `Restock` (GET/POST); ajustar `Save` y `Delete`.
- **Vistas:** `SpareParts/Restock`, historial de entradas en `SpareParts/Details` y botón «Stock bajo» en `SpareParts/Index`.
- **Responsable:** Ricardo.

### 4.4 Entrega y kilometraje

- **Qué hace:** como `Admin` quiero registrar la entrega y el kilometraje para tener un historial fiable de cada vehículo.
- **Reglas:**
  - Solo se entrega una reparación completada, y se guarda la fecha.
  - El kilometraje de entrada no puede ser menor que el del vehículo.
  - Al abrir la reparación se actualiza el kilometraje del vehículo.
  - Una reparación completada o entregada no se elimina ni cambia sus horas o tarifa (resuelve E2 y E8).
- **Modelo:** `DeliveredAt?` en `Repair`. Actualizar los datos de ejemplo.
- **Controlador y acciones:** `RepairsController`: `Create`, `Edit`, `Deliver` y `Delete`; reglas en `RepairAuthorizationHandler`.
- **Vistas:** fecha de entrega en `Repairs/Details` y en el historial de `Vehicle/Details`.
- **Responsable:** Rubén.

## 5. Qué más nos falta

| Área | Pendiente |
|---|---|
| Validaciones | VIN de 17 caracteres y formato de matrícula. Fecha de apertura no futura. Comprobar el rol del mecánico y del propietario. Mensajes en español. |
| Permisos | Decidir qué ve el rol `User` en el menú Taller. Revisar lo que puede cambiar `Admin` en reparaciones cerradas. |
| Diseño | Colores por estado de reparación. Búsqueda y paginación en vehículos, reparaciones y recambios. Revisar la vista en móvil. Unificar el nombre de la aplicación. Ajustar los textos de la portada. |
| Datos de prueba | Añadir citas, presupuestos y entradas de stock. Cargar las cuentas de demostración solo en desarrollo. |
| README y documentación | Corregir puerto, bloqueo y permisos. Declarar licencia. Unificar el flujo de ramas entre `CONTRIBUTING.md` y `.github/copilot-instructions.md`. |
| Repositorio | `App_Data/g3.db.bak` no está cubierto por `.gitignore`. Revisar los archivos sin seguimiento antes del próximo commit. |

## 6. Reparto por persona

Cada persona corrige primero sus errores y después desarrolla su funcionalidad. El área se ha propuesto según el historial de commits.

### Iván — usuarios, reparaciones, seguridad y citas

1. E1: eliminar usuarios con datos asociados.
2. E7 y E9: mensajes en español y cuentas de demostración solo en desarrollo.
3. Funcionalidad 4.1, solicitud de cita.
4. E10: documentación y README.


### Ricardo — recambios, almacén y presupuesto

1. Botón de filtro de stock bajo.
2. Funcionalidad 4.3, reposición de stock.
3. Búsqueda y paginación en recambios.

### Rubén — vehículos, presentador e interfaz

1. E6: validaciones del vehículo.
2. Funcionalidad 4.4, entrega y kilometraje (incluye E2 y E8).
3. Diseño: colores por estado, textos de la portada y nombre de la aplicación.

**Dependencias:** 4.1, 4.2 y 4.4 tocan `Repair` o `RepairsController`. Conviene fusionar primero los cambios de modelo y trabajar con pull requests pequeñas.

## 7. Si sobra tiempo

- Migraciones de EF Core para no perder los datos en cada arranque.
- Avisos por correo al cliente (cita aceptada, presupuesto, vehículo listo) y confirmación de email real.
- Factura en PDF con IVA.
- Panel con indicadores: ingresos, reparaciones por mecánico y recambios más usados.
- Calendario de citas y carga de trabajo por mecánico.
- Fotos de la reparación.
- Código QR y códigos de recuperación para 2FA.
- Despliegue con base de datos persistente.

## 8. Dudas, ruegos y preguntas

- ¿Aceptamos las cuatro funcionalidades del apartado 4 como MVP? ¿Sobra o falta alguna?
- ¿Cuál es la fecha de entrega y qué hitos intermedios queremos?
- ¿El presupuesto es obligatorio en todas las reparaciones o solo a partir de un importe?
- ¿Qué pasa si el coste real supera el presupuesto aprobado?
- ¿Debe `Admin` poder corregir una reparación entregada?
- ¿El rol `User` se mantiene o quien se registra pasa directamente a `Customer`?
- ¿Trabajamos con ramas y pull requests o directamente sobre `main`? ¿Quién revisa?
