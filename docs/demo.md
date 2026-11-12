# Demostración

Cuentas de ejemplo y guion para recorrer la aplicación con los datos de ejemplo.

Documentos relacionados: [README](../README.md) · [Guía funcional](guia-funcional.md) · [Modelo de datos](diagrama-er.md).

> [!WARNING]
> Estas cuentas son de ejemplo y solo para desarrollo.
> No las uses en producción.
> El seed debería cargarse únicamente en el entorno `Development`.
> <!-- TODO: hoy el seed se controla con `SeedData:Enabled` (por defecto `true` en `appsettings.json`) y no depende del entorno. Valorar condicionarlo a `IsDevelopment()`. -->
> Fuera de desarrollo, desactívalo con `SeedData__Enabled=false`.

## Antes de empezar

1. Arranca la aplicación como se indica en el [README](../README.md#instalación-y-ejecución).
2. Cada arranque borra y recrea la base de datos y vuelve a cargar los datos de ejemplo.
3. Abre <http://localhost:5107>.

## Cuentas de ejemplo

Las define [`Data/DbInitializer.cs`](../Data/DbInitializer.cs).

| Perfil | Usuario | Contraseña | Nombre visible |
|---|---|---|---|
| `Admin` | `admin` | `Admin123!` | Administrador |
| `Mechanic` | `mechanic` | `Mechanic123!` | Mecánico Demo |
| `Mechanic` | `mechanic2` | `Mechanic123!` | Raúl Ortega |
| `Mechanic` | `mechanic3` | `Mechanic123!` | Elena Navarro |
| `Customer` | `customer` | `Customer123!` | Cliente Demo |
| `Customer` | `customer2` | `Customer123!` | Lucía Fernández |
| `Customer` | `customer3` | `Customer123!` | Marcos Ruiz |
| `User` | `user` | `User123!` | Usuario de demo |

## Datos de ejemplo

- 7 vehículos repartidos entre los tres clientes.
- 9 recambios.
- 13 reparaciones con todos los estados y varios tipos.

| Cliente | Matrículas |
|---|---|
| `customer` | `1234ABC`, `5678DEF`, `9012GHI` |
| `customer2` | `3456JKL`, `7890MNP` |
| `customer3` | `2468RST`, `1357VWX` |

## Guion

### 1. Administración de cuentas

1. Inicia sesión con `admin`.
2. Abre **Administración de usuarios** y busca una cuenta.
3. Edita la cuenta `user` y cambia su rol.
4. Intenta eliminar tu propia cuenta. La aplicación lo rechaza con un mensaje.

### 2. Vista del cliente

1. Cierra sesión e inicia con `customer`.
2. Abre **Taller → Vehículos**. Solo aparecen `1234ABC`, `5678DEF` y `9012GHI`.
3. Abre **Taller → Reparaciones** y entra en el detalle de una. Se ven mecánico, estado, piezas y coste total.
4. Abre **Taller → Recambios** para consultar el catálogo.
5. Inicia con `customer2` y comprueba que ve otros vehículos.

### 3. Trabajo del mecánico

1. Inicia con `mechanic`.
2. Crea un vehículo en **Taller → Vehículos** y asígnalo a un cliente.
3. Crea una reparación para ese vehículo. Elige tipo y estado, e indica horas y tarifa.
4. Anota el stock de un recambio en **Taller → Recambios**.
5. Abre **Taller → Piezas usadas** y crea una nueva. Elige la reparación, el recambio y la cantidad.
6. Vuelve a **Recambios**. El stock ha bajado en la cantidad añadida.
7. Prueba a añadir más unidades de las que hay. La aplicación avisa de que no hay stock suficiente.
8. Elimina la línea desde **Taller → Piezas usadas**. El stock se repone.

### 4. Protección del historial

1. Intenta eliminar el vehículo `1234ABC` o cualquiera con reparaciones. La aplicación lo impide.
2. Intenta eliminar un recambio que figure en alguna reparación. La aplicación lo impide.

### 5. Precio histórico

1. Abre la reparación de `1234ABC` con el cambio de aceite. La línea del aceite 5W30 figura a 36,90 €.
2. Abre el recambio «Aceite 5W30 5L». El catálogo figura a 38,90 €.
3. Cambia el precio del recambio.
4. Vuelve a la reparación. El precio de la línea no cambia.

### 6. Registro y tema

1. Cierra sesión y usa **Crear cuenta**. El único rol disponible es `User`.
2. Edita el perfil y sube un avatar.
3. Cambia entre tema claro y oscuro con el botón de la barra.
