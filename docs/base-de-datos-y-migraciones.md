# Base de datos y migraciones

La aplicación usa Entity Framework Core con el proveedor SQLite. El contexto [`ApplicationDbContext`](../Data/ApplicationDbContext.cs) hereda de `IdentityDbContext<ApplicationUser>` y reúne las tablas de Identity y las entidades del taller.

Documentos relacionados: [Modelo de datos](diagrama-er.md) · [Arquitectura](arquitectura.md) · [Configuración](configuracion.md).

## Entidades y relaciones

- `ApplicationUser` representa las cuentas y se relaciona con sus vehículos y reparaciones asignadas a mecánicos.
- `Vehicle` tiene un propietario y puede tener varias reparaciones.
- `Repair` pertenece a un vehículo y referencia un mecánico.
- `RepairPart` enlaza una reparación con un recambio, guardando cantidad y precio unitario histórico.
- `SparePart` representa el catálogo y su stock.

`Repair` y `SparePart` forman una relación N:M mediante `RepairPart`. La configuración de borrado es `Restrict` para las relaciones históricas y `Cascade` entre `Repair` y sus líneas `RepairPart`. La matrícula (`Vehicle.LicensePlate`) y la referencia del recambio (`SparePart.Reference`) tienen índice único. El diagrama ER y las propiedades completas están en [Modelo de datos](diagrama-er.md).

## Ciclo de vida de la base

Al arrancar, [`DbInitializer`](../Data/DbInitializer.cs) llama a `EnsureDeleted()` y `EnsureCreated()`. Esto elimina la base configurada y la vuelve a crear a partir del modelo actual. Después, si `SeedData:Enabled` está habilitado, crea roles, usuarios y filas de demostración.

**Este borrado ocurre también cuando el seed está desactivado.** En ese caso tampoco se crean los roles. No ejecutes la aplicación con una base que contenga datos que quieras conservar. El contenedor tampoco configura persistencia para SQLite.

## Migraciones

El proyecto no tiene migraciones de EF Core y no ejecuta `dotnet ef database update`, aunque [`.config/dotnet-tools.json`](../.config/dotnet-tools.json) declara la herramienta `dotnet-ef`. No generes migraciones para el flujo actual: los cambios de esquema se hacen en las entidades y en `ApplicationDbContext`, y el inicializador recrea el esquema al inicio.

Para modificar una relación:

1. Actualiza la entidad en `Models/`.
2. Configura la relación y su comportamiento `OnDelete` en `ApplicationDbContext`.
3. Ajusta los datos demo en `DbInitializer`, si corresponde.
4. Actualiza las pruebas y el [diagrama ER](diagrama-er.md).

El flujo de vehículo → reparación → piezas y el ajuste de stock están descritos en el [modelo de datos](diagrama-er.md#flujo-de-datos).
