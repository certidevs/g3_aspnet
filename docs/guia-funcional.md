# Guía funcional

Describe qué resuelve TALLER .NET, qué puede hacer cada perfil y qué reglas aplica la aplicación. Todo lo indicado se ha contrastado con el código del repositorio.

Documentos relacionados: [README](../README.md) · [Modelo de datos](diagrama-er.md) · [Cuentas y guion de demostración](demo.md).

## Qué resuelve la aplicación

TALLER .NET centraliza la gestión de un taller mecánico:

- Alta de clientes y de sus vehículos.
- Seguimiento de cada reparación, desde que se abre hasta que se entrega.
- Control del almacén de recambios y de las piezas usadas en cada reparación.
- Área privada en la que el cliente consulta sus vehículos y sus reparaciones.

## Perfiles

La aplicación define cuatro roles: `Admin`, `Mechanic`, `Customer` y `User`.

### Admin

- Gestiona las cuentas desde **Administración de usuarios**: buscar, crear, editar, asignar rol, activar/desactivar y eliminar.
- No puede quitarse a sí mismo el rol `Admin`, desactivar su cuenta ni eliminarla.
- No se puede eliminar al último administrador.
- Gestiona vehículos, reparaciones, piezas usadas y recambios.

### Mechanic

- Consulta todos los vehículos del taller y puede crearlos, editarlos y eliminarlos.
- Abre y actualiza reparaciones: vehículo, mecánico asignado, tipo, estado, fechas, kilometraje, horas y tarifa.
- Añade piezas a una reparación. El stock se ajusta de forma automática.
- Consulta y gestiona el catálogo de recambios.

### Customer

- Consulta solo sus propios vehículos y las reparaciones de esos vehículos.
- Ve en cada reparación el mecánico, el estado, el kilometraje de entrada, las piezas usadas y el coste total.
- Consulta el catálogo de recambios.
- No puede crear, editar ni eliminar vehículos.

### User

- Es el rol que recibe cualquier persona que se registra desde **Crear cuenta**. El registro no permite elegir otro.
- Un administrador puede cambiarle el rol desde **Administración de usuarios**.
- Puede iniciar sesión y gestionar su perfil: nombre visible, avatar y contraseña.

### Todos los perfiles

- Cambio de tema claro u oscuro, con la preferencia guardada en el navegador.
- Edición de perfil y cambio de contraseña.
- Contraseña de al menos 8 caracteres, con un dígito y un carácter no alfanumérico.
- Bloqueo de la cuenta durante 10 minutos tras 5 intentos de acceso fallidos.

## Permisos por perfil

La tabla refleja lo que el código aplica hoy.

| Funcionalidad | Admin | Mechanic | Customer | User |
|---|:---:|:---:|:---:|:---:|
| Administración de usuarios | Sí | No | No | No |
| Gestionar el perfil propio | Sí | Sí | Sí | Sí |
| Consultar vehículos | Todos | Todos | Solo propios | Todos |
| Crear, editar y eliminar vehículos | Sí | Sí | No | No |
| Consultar reparaciones | Todas | Todas | Solo de sus vehículos | Todas |
| Crear, editar y eliminar reparaciones; añadir piezas | Sí | Sí | Sí (1) | Sí (1) |
| Consultar y gestionar piezas usadas | Sí | Sí | Sí (1) | Sí (1) |
| Consultar y gestionar recambios | Sí (2) | Sí (2) | Sí (2) | Sí (2) |

Notas:

1. <!-- TODO: confirmar el comportamiento previsto. Hoy `RepairsController` y `RepairPartsController` solo exigen sesión iniciada; no comprueban el rol al crear, editar o eliminar. Además, `Edit` y `Delete` de reparaciones no filtran por propietario. -->
   Estas acciones no están restringidas por rol.
2. <!-- TODO: confirmar el comportamiento previsto. `SparePartsController` no tiene `[Authorize]`. -->
   El catálogo de recambios no exige iniciar sesión, tampoco para crear, editar o eliminar.

## Reglas de negocio

### Stock automático

- Al añadir una pieza a una reparación, el stock del recambio baja en la cantidad indicada.
- Si no hay stock suficiente, la aplicación muestra un aviso y no guarda la pieza.
- Al editar la cantidad o cambiar el recambio de una línea, el stock se recalcula.
- Al eliminar una línea o una reparación, las unidades vuelven al almacén.
- La cantidad por línea debe estar entre 1 y 1000.

### Precio histórico

- Cada línea de reparación guarda el precio unitario del momento en que se añade la pieza.
- Un cambio posterior del precio en el catálogo no modifica las reparaciones ya registradas.

### Coste de la reparación

La aplicación no guarda un total. Lo calcula al consultar la reparación:

```text
Coste = (LaborHours × HourlyRate) + Σ(Quantity × UnitPrice)
```

### Protección del historial

- No se puede eliminar un vehículo que tenga reparaciones.
- No se puede eliminar un recambio que se haya usado en alguna reparación.
- La base de datos impide borrar una cuenta que tenga vehículos o reparaciones asignadas.
- Eliminar una reparación elimina también sus líneas de piezas.

### Datos únicos

- La matrícula del vehículo no se puede repetir.
- La referencia del recambio no se puede repetir.

## Estados y tipos de reparación

Los valores están en [Modelo de datos](diagrama-er.md#valores-de-los-enums).

## Limitaciones conocidas

- <!-- TODO: confirmar si se quiere avisar del stock bajo. Existe `MinStock`, pero no hay ninguna alerta en la interfaz (hay un TODO en `SparePartsController`). -->
  `MinStock` se guarda, pero la aplicación no avisa cuando el stock llega al mínimo.
- <!-- TODO: el detalle de reparación no tiene aún formulario «Añadir pieza» (TODO en `Views/Repairs/Details.cshtml`). -->
  Las piezas se añaden desde el módulo **Piezas usadas**.
