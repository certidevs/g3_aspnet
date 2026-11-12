# Problemas frecuentes

Errores habituales al ejecutar o usar la aplicación, con su causa y su solución.

## Ejecución

### El puerto 5107 está ocupado

Otra aplicación usa el puerto. Cierra el proceso o cambia `applicationUrl` en [`Properties/launchSettings.json`](../Properties/launchSettings.json).
En Docker, el puerto es el de la variable `PORT` o, si no existe, el `10000`.

### `dotnet` no encuentra el SDK

[`global.json`](../global.json) exige el SDK `10.0.100` o una versión posterior de la misma línea.
No admite versiones preliminares.
Comprueba tu versión con `dotnet --list-sdks` e instala el SDK 10 si falta. La [guía de IDE](IDE-SETUP.md) explica cómo.

### Con el perfil `https` el navegador avisa del certificado

Falta confiar en el certificado de desarrollo:

```bash
dotnet dev-certs https --trust
```

El perfil `http` no lo necesita.

### Los datos desaparecen al reiniciar

Es el comportamiento previsto. Cada arranque ejecuta `EnsureDeleted` y `EnsureCreated`.
Esto ocurre aunque `SeedData:Enabled` sea `false`.
En Docker tampoco hay persistencia para SQLite.

### No debo generar migraciones

El proyecto no usa migraciones de EF Core.
Un cambio de esquema es un cambio en las entidades y en [`ApplicationDbContext`](../Data/ApplicationDbContext.cs).

## Cuentas y acceso

### No puedo iniciar sesión

- Comprueba el usuario y la contraseña. Las cuentas de ejemplo están en [Demostración](demo.md).
- Tras 5 intentos fallidos, la cuenta se bloquea 10 minutos. Reiniciar la aplicación también la desbloquea, porque la base se recrea.
- Una cuenta desactivada por un administrador pierde la sesión.

### Al registrarme solo puedo elegir `User`

Es intencionado. El registro público solo permite ese rol.
Un administrador puede cambiarlo desde **Administración de usuarios**.

### La contraseña no se acepta

Debe tener al menos 8 caracteres, un dígito y un carácter no alfanumérico.

### El avatar se rechaza

El archivo debe pesar como máximo 5 MiB y tener una de estas extensiones: `.jpg`, `.jpeg`, `.png`, `.gif` o `.webp`.
La aplicación también comprueba el contenido real del archivo.
Los límites están en `FileUploads:Images`, en [`appsettings.json`](../appsettings.json).

## Uso de la aplicación

### Un cliente no ve ningún vehículo

Un `Customer` solo ve los vehículos cuyo propietario es su cuenta.
Revisa el propietario del vehículo.

### No aparece ningún mecánico o propietario en el desplegable

El de mecánico lista solo las cuentas con rol `Mechanic`.
El de propietario lista solo las cuentas con rol `Customer`.
Asigna el rol desde **Administración de usuarios**.

### No se puede eliminar un vehículo, un recambio o una cuenta

- Un vehículo con reparaciones no se elimina.
- Un recambio usado en alguna reparación no se elimina.
- Una cuenta con vehículos o reparaciones asignadas no se elimina.
- Tampoco puedes eliminar tu propia cuenta ni al último administrador.

Es la protección del historial. Consulta la [guía funcional](guia-funcional.md#protección-del-historial).

### «No hay stock suficiente»

La cantidad pedida supera el stock del recambio.
Reduce la cantidad o aumenta el stock del recambio desde **Taller → Recambios**.

### Un recambio duplicado o una matrícula repetida

La referencia del recambio y la matrícula del vehículo son únicas.
Usa otra, o edita el registro existente.
